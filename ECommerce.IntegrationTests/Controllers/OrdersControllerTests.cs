using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.IntegrationTests.Common;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.IntegrationTests.Controllers
{
    public class OrdersControllerTests : IntegrationTestBase
    {
        public OrdersControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetAll_ReturnsPagedOrders()
        {
            ClearTable<Order>();
            await SeedOrderAsync();
            await SeedOrderAsync();

            var response = await Client.GetAsync("api/orders");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content
                .ReadFromJsonAsync<PagedResponse<OrderResponse>>();
            body!.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetAll_FilterByStatus_ReturnsOnlyMatchingOrders()
        {
            ClearTable<Order>();
            await SeedOrderAsync(status: OrderStatus.Pending);
            await SeedOrderAsync(status: OrderStatus.Pending);
            await SeedOrderAsync(status: OrderStatus.Confirmed);

            var response = await Client.GetAsync("/api/orders?status=1");

            var body = await response.Content
                .ReadFromJsonAsync<PagedResponse<OrderResponse>>();
            body!.Data.Should().OnlyContain(o => o.Status == OrderStatus.Pending);
        }

        [Fact]
        public async Task GetAll_FilterByCustomerId_ReturnsOnlyThatCustomersOrders()
        {
            ClearTable<Order>();
            var (customer, _, _) = await SeedOrderAsync();
            await SeedOrderAsync();

            var response = await Client.GetAsync($"/api/orders?customerId={customer.Id}");

            var body = await response.Content
                .ReadFromJsonAsync<PagedResponse<OrderResponse>>();
            body!.Data.Should().OnlyContain(o => o.CustomerId == customer.Id);
        }

        [Fact]
        public async Task GetById_ExistingOrder_ReturnsFullDetail()
        {
            var (customer, product, order) = await SeedOrderAsync();

            var response = await Client.GetAsync($"/api/orders/{order.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<OrderResponse>();
            body!.Id.Should().Be(order.Id);
            body.CustomerId.Should().Be(customer.Id);
            body.Items.Should().HaveCount(1);
            body.Items[0].ProductId.Should().Be(product.Id);
        }

        [Fact]
        public async Task GetById_NonExistent_Returns404()
        {
            var response = await Client.GetAsync($"/api/orders/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_ValidRequest_Returns201AndReservesStock()
        {
            var category = new Category
            {
                Name = "Test",
                Slug = $"ord-{Guid.NewGuid():N}"
            };
            DbContext.Categories.Add(category);

            var customer = new Customer
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = $"jane_{Guid.NewGuid():N}@example.com",
                IsActive = true,
            };
            DbContext.Customers.Add(customer);

            var product = new Product
            {
                CategoryId = category.Id,
                Name = "Orderable Product",
                Sku = $"ORD-{Guid.NewGuid().ToString("N")[..8]}".ToUpperInvariant(),
                Price = 100m,
                StockQuantity = 50,
                IsActive = true,
            };
            DbContext.Products.Add(product);
            await DbContext.SaveChangesAsync();

            var request = new CreateOrderRequest
            {
                CustomerId = customer.Id,
                Items = new() { new() { ProductId = product.Id, Quantity = 3 } },
                ShippingAddress = new()
                {
                    Street = "456 Test Ave",
                    City = "Santo Domingo",
                    State = "DN",
                    ZipCode = "10101",
                    Country = "DO"
                }
            };

            var response = await Client.PostAsJsonAsync("/api/orders", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var body = await response.Content.ReadFromJsonAsync<OrderResponse>();
            body!.Status.Should().Be(OrderStatus.Pending);
            body.Items.Should().HaveCount(1);
            body.Total.Should().BeGreaterThan(0);

            var updatedProduct = await ReloadAsync<Product>(product.Id);
            updatedProduct!.ReservedQuantity.Should().Be(3);
        }

        [Fact]
        public async Task Create_InsufficientStock_Returns422()
        {
            var category = new Category
            {
                Name = "Test",
                Slug = $"ins-{Guid.NewGuid():N}"
            };
            DbContext.Categories.Add(category);

            var customer = new Customer
            {
                FirstName = "Low",
                LastName = "Stock",
                Email = $"low_{Guid.NewGuid():N}@example.com",
                IsActive = true
            };
            DbContext.Customers.Add(customer);

            var product = new Product
            {
                CategoryId = category.Id,
                Name = "Low Stock Item",
                Sku = $"LOW-{Guid.NewGuid().ToString("N")[..8]}".ToUpperInvariant(),
                Price = 10m,
                StockQuantity = 1,
                IsActive = true
            };
            DbContext.Products.Add(product);
            await DbContext.SaveChangesAsync();

            var request = new CreateOrderRequest
            {
                CustomerId = customer.Id,
                Items = new() { new() { ProductId = product.Id, Quantity = 10 } },
                ShippingAddress = new()
                {
                    Street = "123 Main",
                    City = "SD",
                    State = "DN",
                    ZipCode = "10101",
                    Country = "DO"
                }
            };

            var response = await Client.PostAsJsonAsync("/api/orders", request);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            var updatedProduct = await ReloadAsync<Product>(product.Id);
            updatedProduct!.ReservedQuantity.Should().Be(0);
        }

        [Fact]
        public async Task Create_InvalidRequest_EmptyItems_Returns400()
        {
            var request = new CreateOrderRequest
            {
                CustomerId = Guid.NewGuid(),
                Items = new(),
                ShippingAddress = new()
                {
                    Street = "123 Main",
                    City = "SD",
                    State = "DN",
                    ZipCode = "10101",
                    Country = "DO"
                }
            };

            var response = await Client.PostAsJsonAsync("/api/orders", request);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_DuplicateProductIds_Returns400()
        {
            var productId = Guid.NewGuid();
            var request = new CreateOrderRequest
            {
                CustomerId = Guid.NewGuid(),
                Items = new()
                {
                    new() { ProductId = productId, Quantity = 1 },
                    new() { ProductId = productId, Quantity = 2 },
                },
                ShippingAddress = new()
                {
                    Street = "123 Main",
                    City = "SD",
                    State = "DN",
                    ZipCode = "10101",
                    Country = "DO"
                }
            };

            var response = await Client.PostAsJsonAsync("/api/orders", request);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateStatus_ValidTransition_Returns200AndUpdatesStatus()
        {
            var (_, _, order) = await SeedOrderAsync();

            var response = await Client.PatchAsJsonAsync(
                $"/api/orders/{order.Id}/status",
                new UpdateOrderStatusRequest
                {
                    NewStatus = OrderStatus.Confirmed,
                    Notes = "Payment verified"
                });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<OrderResponse>();
            body!.Status.Should().Be(OrderStatus.Confirmed);
            body.StatusHistory.Should().HaveCount(1);
            body.StatusHistory[0].FromStatus.Should().Be(OrderStatus.Pending);
            body.StatusHistory[0].ToStatus.Should().Be(OrderStatus.Confirmed);
        }

        [Fact]
        public async Task UpdateStatus_ConfirmOrder_DeductsStockFromDatabase()
        {
            var (_, product, order) = await SeedOrderAsync(
                stockQuantity: 100, reservedQuantity: 2);

            await Client.PatchAsJsonAsync(
                $"/api/orders/{order.Id}/status",
                new UpdateOrderStatusRequest { NewStatus = OrderStatus.Confirmed });

            var updatedProduct = await ReloadAsync<Product>(product.Id);
            updatedProduct!.StockQuantity.Should().Be(98);
            updatedProduct.ReservedQuantity.Should().Be(0);
        }

        [Fact]
        public async Task UpdateStatus_InvalidTransition_Returns400()
        {
            var (_, _, order) = await SeedOrderAsync(status: OrderStatus.Delivered);

            var response = await Client.PatchAsJsonAsync(
                $"/api/orders/{order.Id}/status",
                new UpdateOrderStatusRequest { NewStatus = OrderStatus.Cancelled });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateStatus_SkipStates_Returns404()
        {
            var (_, _, order) = await SeedOrderAsync();

            var response = await Client.PatchAsJsonAsync(
                $"/api/orders/{order.Id}/status",
                new UpdateOrderStatusRequest { NewStatus = OrderStatus.Shipped });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateStatus_NonExistentOrder_Returns404()
        {
            var response = await Client.PatchAsJsonAsync(
                $"/api/orders/{Guid.NewGuid()}/status",
                new UpdateOrderStatusRequest { NewStatus = OrderStatus.Confirmed });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Cancel_PendingOrder_Returns200AndReleasesStock()
        {
            var (_, product, order) = await SeedOrderAsync(
                stockQuantity: 100, reservedQuantity: 2);

            var response = await Client.DeleteAsync(
                $"/api/orders/{order.Id}?reason=Customer+request");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedProduct = await ReloadAsync<Product>(product.Id);
            updatedProduct!.ReservedQuantity.Should().Be(0);
            updatedProduct.StockQuantity.Should().Be(100);

            var check = await Client.GetAsync($"/api/orders/{order.Id}");
            var body = await check.Content.ReadFromJsonAsync<OrderResponse>();
            body!.Status.Should().Be(OrderStatus.Cancelled);
        }

        [Fact]
        public async Task Cancel_DeliveredOrder_Returns400()
        {
            var (_, _, order) = await SeedOrderAsync(status: OrderStatus.Delivered);

            var response = await Client.DeleteAsync($"/api/orders/{order.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Cancel_ShippedOrder_Returns400()
        {
            var (_, _, order) = await SeedOrderAsync(status: OrderStatus.Shipped);

            var response = await Client.DeleteAsync($"/api/orders/{order.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Cancel_NonExistentOrder_Returns404()
        {
            var response = await Client.DeleteAsync($"/api/orders/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task MultipleTransitions_AccumulateStatusHistory()
        {
            var (_, _, order) = await SeedOrderAsync();

            await Client.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
                new UpdateOrderStatusRequest { NewStatus = OrderStatus.Confirmed });
            await Client.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
                new UpdateOrderStatusRequest { NewStatus = OrderStatus.Processing });
            await Client.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
                new UpdateOrderStatusRequest { NewStatus = OrderStatus.Packed });

            var response = await Client.GetAsync($"/api/orders/{order.Id}");
            var body = await response.Content.ReadFromJsonAsync<OrderResponse>();

            body!.Status.Should().Be(OrderStatus.Packed);
            body.StatusHistory.Should().HaveCount(3);
            body.StatusHistory[0].FromStatus.Should().Be(OrderStatus.Pending);
            body.StatusHistory[0].ToStatus.Should().Be(OrderStatus.Confirmed);
            body.StatusHistory[2].ToStatus.Should().Be(OrderStatus.Packed);
        }
    }
}
