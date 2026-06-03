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
    public class CategoriesControllerTests : IntegrationTestBase
    {
        public CategoriesControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetAll_ReturnsAllCategories()
        {
            ClearTable<Category>();
            await SeedAsync(new CategoryBuilder().WithSlug("electronics").Build());
            await SeedAsync(new CategoryBuilder().WithName("Books").WithSlug("books").Build());

            var response = await Client.GetAsync("/api/categories");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content
                .ReadFromJsonAsync<IEnumerable<CategoryResponse>>();
            body!.Should().HaveCount(2);
        }

        [Fact]
        public async Task Create_ValidRequest_Returns201()
        {
            var request = new CreateCategoryRequest
            {
                Name = "Clothing",
                Slug = $"clothing-{Guid.NewGuid():N}"
            };

            var response = await Client.PostAsJsonAsync("/api/categories", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var body = await response.Content.ReadFromJsonAsync<CategoryResponse>();
            body!.Slug.Should().Be(request.Slug);
        }

        [Fact]
        public async Task Create_DuplicateSlug_Returns409()
        {
            await SeedAsync(new CategoryBuilder().WithSlug("duplicate-slug").Build());

            var response = await Client.PostAsJsonAsync("/api/categories",
                new CreateCategoryRequest { Name = "X", Slug = "duplicate-slug" });

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Delete_CategoryWithProducts_Returns409()
        {
            var category = await SeedAsync(new CategoryBuilder().WithSlug("has-products").Build());
            await SeedAsync(new ProductBuilder().WithCategoryId(category.Id).Build());

            var response = await Client.DeleteAsync($"/api/categories/{category.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Delete_EmptyCategory_Returns200()
        {
            var category = await SeedAsync(
                new CategoryBuilder().WithSlug($"empty-{Guid.NewGuid():N}").Build());

            var response = await Client.DeleteAsync($"/api/categories/{category.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
