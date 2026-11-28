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
using System.Text.Json;
using System.Threading.Tasks;

namespace ECommerce.IntegrationTests.Controllers
{
    public class ReportsControllerTests : IntegrationTestBase
    {
        public ReportsControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<Product> SeedProductAsync(string name, decimal price)
        {
            var category = new Category { Name = "Reports", Slug = $"rpt-{Guid.NewGuid():N}" };
            DbContext.Categories.Add(category);

            var product = new Product
            {
                CategoryId = category.Id,
                Name = name,
                Sku = $"RPT-{Guid.NewGuid():N}"[..12].ToUpper(),
                Price = price,
                StockQuantity = 1000,
                IsActive = true,
            };
            DbContext.Products.Add(product);
            await DbContext.SaveChangesAsync();
            return product;
        }

        private async Task<Order> SeedOrderOnAsync(
            DateTime createdAt, OrderStatus status, params (Product Product, int Qty)[] lines)
        {
            var customer = new Customer
            {
                FirstName = "Report",
                LastName = "Customer",
                Email = $"rpt_{Guid.NewGuid():N}@example.com",
                IsActive = true,
            };
            DbContext.Customers.Add(customer);

            var subtotal = lines.Sum(l => l.Product.Price * l.Qty);
            var tax = Math.Round(subtotal * 0.18m, 2);

            var order = new Order
            {
                CustomerId = customer.Id,
                Status = status,
                Subtotal = subtotal,
                Tax = tax,
                Total = subtotal + tax,
                ShippingAddressJson = JsonSerializer.Serialize(new
                {
                    street = "1 Report St",
                    city = "Santo Domingo",
                    state = "DN",
                    zipCode = "10101",
                    country = "DO"
                }),
                Items = lines.Select(l => new OrderItem
                {
                    ProductId = l.Product.Id,
                    Quantity = l.Qty,
                    UnitPrice = l.Product.Price
                }).ToList()
            };
            DbContext.Orders.Add(order);
            await DbContext.SaveChangesAsync();

            DbContext.Entry(order).Property(o => o.CreatedAt).CurrentValue = createdAt;
            await DbContext.SaveChangesAsync();

            return order;
        }

        private static DateTime Utc(int y, int m, int d, int h = 12) =>
            new(y, m, d, h, 0, 0, DateTimeKind.Utc);

        private async Task<DashboardResponse> GetDashboard(string query)
        {
            var response = await Client.GetAsync($"/api/reports/dashboard?{query}");
            var body = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

            return JsonSerializer.Deserialize<DashboardResponse>(
                body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        }

        [Fact]
        public async Task Dashboard_CountsOnlyPaidOrdersInsideTheRange()
        {
            var p = await SeedProductAsync("Widget", 50m);

            await SeedOrderOnAsync(Utc(2020, 1, 10), OrderStatus.Delivered, (p, 2));
            await SeedOrderOnAsync(Utc(2020, 1, 10), OrderStatus.Delivered, (p, 2));
            await SeedOrderOnAsync(Utc(2020, 1, 11), OrderStatus.Pending, (p, 2));
            await SeedOrderOnAsync(Utc(2020, 1, 12), OrderStatus.Shipped, (p, 2));
            await SeedOrderOnAsync(Utc(2020, 1, 13), OrderStatus.Delivered, (p, 2));

            var d = await GetDashboard("from=2020-01-10&to=2020-01-12");

            d.Summary.OrderCount.Should().Be(3);
            d.Summary.GrossRevenue.Should().Be(354m);
            d.Summary.TaxCollected.Should().Be(54m);
            d.Summary.NetRevenue.Should().Be(300m);
            d.Summary.AverageOrderValue.Should().Be(118m);

            d.DailySales.Select(x => x.OrderCount).Should().Equal(2, 0, 1);
            d.DailySales[0].Revenue.Should().Be(236m);

            d.StatusBreakdown.Single(s => s.Status == OrderStatus.Delivered).Count.Should().Be(2);
            d.StatusBreakdown.Single(s => s.Status == OrderStatus.Pending).Count.Should().Be(1);
            d.StatusBreakdown.Single(s => s.Status == OrderStatus.Shipped).Count.Should().Be(1);
            d.StatusBreakdown.Single(s => s.Status == OrderStatus.Cancelled).Count.Should().Be(0);
        }

        [Fact]
        public async Task Dashboard_RangeEndIsInclusiveUntilMidnight()
        {
            var p = await SeedProductAsync("Late", 10m);
            await SeedOrderOnAsync(Utc(2020, 4, 1, h: 23), OrderStatus.Delivered, (p, 1));

            var d = await GetDashboard("from=2020-04-01&to=2020-04-01");

            d.Summary.OrderCount.Should().Be(1);
            d.DailySales.Should().ContainSingle();
        }

        [Fact]
        public async Task Dashboard_TopProducts_AggregatesAcrossOrdersAndExcludesUnpaid()
        {
            var a = await SeedProductAsync("Product A", 100m);
            var b = await SeedProductAsync("Product B", 10m);
            var c = await SeedProductAsync("Product C", 500m);

            await SeedOrderOnAsync(Utc(2020, 2, 1), OrderStatus.Delivered, (a, 3), (b, 5));
            await SeedOrderOnAsync(Utc(2020, 2, 1), OrderStatus.Processing, (a, 1));
            await SeedOrderOnAsync(Utc(2020, 2, 1), OrderStatus.Cancelled, (c, 9));

            var d = await GetDashboard("from=2020-02-01&to=2020-02-01");

            d.TopProducts.Should().HaveCount(2);
            d.TopProducts[0].ProductId.Should().Be(a.Id);
            d.TopProducts[0].UnitsSold.Should().Be(4);
            d.TopProducts[0].Revenue.Should().Be(400m);
            d.TopProducts[1].ProductId.Should().Be(b.Id);
            d.TopProducts[1].Revenue.Should().Be(50m);
            d.TopProducts.Should().NotContain(t => t.ProductId == c.Id);
        }

        [Fact]
        public async Task Dashboard_TopParameter_LimitsResults()
        {
            var big = await SeedProductAsync("Big", 200m);
            var small = await SeedProductAsync("Small", 5m);
            await SeedOrderOnAsync(Utc(2020, 3, 1), OrderStatus.Delivered, (big, 1), (small, 1));

            var d = await GetDashboard("from=2020-03-01&to=2020-03-01&top=1");

            d.TopProducts.Should().ContainSingle()
             .Which.ProductId.Should().Be(big.Id);
        }

        [Fact]
        public async Task Dashboard_NoDates_Returns30DayContinuousSeries()
        {
            var d = await GetDashboard("");

            d.DailySales.Should().HaveCount(30);
            d.To.DayNumber.Should().Be(d.From.DayNumber + 29);
        }

        [Fact]
        public async Task Dashboard_FromAfterTo_Returns400()
        {
            var response = await Client.GetAsync(
                "/api/reports/dashboard?from=2020-05-10&to=2020-05-01");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Dashboard_RangeTooLong_Returns400()
        {
            var response = await Client.GetAsync(
                "/api/reports/dashboard?from=2019-01-01&to=2020-12-31");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Dashboard_InvalidTop_Returns400()
        {
            var response = await Client.GetAsync("/api/reports/dashboard?top=0");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
