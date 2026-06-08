using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;
using ECommerce.IntegrationTests.Common;
using ECommerce.TestHelpers.Builders;
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
    public class ProductsControllerTests : IntegrationTestBase
    {
        private readonly Guid _categoryId;
        public ProductsControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            var category = new CategoryBuilder().WithSlug($"test-cat-{Guid.NewGuid():N}").Build();
            DbContext.Categories.Add(category);
            DbContext.SaveChanges();
            _categoryId = category.Id;
        }

        [Fact]
        public async Task GetAll_ReturnsPagedProducts()
        {
            ClearTable<Product>();
            foreach (var product in ProductBuilder.BuildMany(3, _categoryId))
                await SeedAsync(product);

            var response = await Client.GetAsync("/api/products");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content
                .ReadFromJsonAsync<PagedResponse<ProductResponse>>();
            body!.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetAll_InStockOnly_FiltersCorrectly()
        {
            ClearTable<Product>();
            await SeedAsync(new ProductBuilder()
                .WithSku($"IN-{Guid.NewGuid().ToString("N")[..6]}").WithStock(10)
                .WithCategoryId(_categoryId).Build());
            await SeedAsync(new ProductBuilder()
                .WithSku($"OUT-{Guid.NewGuid().ToString("N")[..6]}").WithStock(0)
                .WithCategoryId(_categoryId).Build());

            var response = await Client.GetAsync("api/products?inStockOnly=true");
            var body = await response.Content
                .ReadFromJsonAsync<PagedResponse<ProductResponse>>();

            body!.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetById_ExistingProduct_Returns200()
        {
            var product = await SeedAsync(
                new ProductBuilder().WithSku($"GET-{Guid.NewGuid().ToString("N")[..8]}")
                                    .WithCategoryId(_categoryId).Build());

            var response = await Client.GetAsync($"/api/products/{product.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<ProductResponse>();
            body!.Id.Should().Be(product.Id);
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            var response = await Client.GetAsync($"/api/products/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_ValidProduct_Returns201()
        {
            var request = new CreateProductRequest
            {
                CategoryId = _categoryId,
                Name = "Integration Test Product",
                Sku = $"INT-{Guid.NewGuid().ToString("N")[..8]}".ToUpper(),
                Price = 99.99m,
                StockQuantity = 25
            };

            var response = await Client.PostAsJsonAsync("/api/products", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var body = await response.Content.ReadFromJsonAsync<ProductResponse>();
            body!.StockQuantity.Should().Be(25);
        }

        [Fact]
        public async Task Create_InvalidPrice_Returns400()
        {
            var request = new CreateProductRequest
            {
                CategoryId = _categoryId,
                Name = "Bad",
                Sku = "BAD-001",
                Price = -1,
                StockQuantity = 0
            };

            var response = await Client.PostAsJsonAsync("/api/products", request);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AdjustStock_ValidDelta_UpdatesStock()
        {
            var product = await SeedAsync(
                new ProductBuilder().WithSku($"STK-{Guid.NewGuid().ToString("N")[..8]}")
                                    .WithStock(50).WithCategoryId(_categoryId).Build());

            var response = await Client.PatchAsJsonAsync(
                $"/api/products/{product.Id}/stock",
                new AdjustStockRequest { Quantity = 25, Reason = "Restock" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<ProductResponse>();
            body!.StockQuantity.Should().Be(75);
        }

        [Fact]
        public async Task AdjustStock_WouldGoBelowZero_Returns400()
        {
            var product = await SeedAsync(
                new ProductBuilder().WithSku($"NEG-{Guid.NewGuid().ToString("N")[..8]}")
                                    .WithStock(5).WithCategoryId(_categoryId).Build());

            var response = await Client.PatchAsJsonAsync(
                $"/api/products/{product.Id}/stock",
                new AdjustStockRequest { Quantity = -100, Reason = "Error" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Deactivate_ActiveProduct_Returns200AndIsInactive()
        {
            var product = await SeedAsync(
                new ProductBuilder().WithSku($"DEACT-{Guid.NewGuid().ToString("N")[..6]}")
                                    .WithCategoryId(_categoryId).Build());

            var response = await Client.PatchAsync(
                $"/api/products/{product.Id}/deactivate", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var check = await Client.GetAsync($"/api/products/{product.Id}");
            var body = await check.Content.ReadFromJsonAsync<ProductResponse>();
            body!.IsActive.Should().BeFalse();
        }



    }
}
