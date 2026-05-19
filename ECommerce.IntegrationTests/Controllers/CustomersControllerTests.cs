using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;
using ECommerce.IntegrationTests.Common;
using ECommerce.UnitTests.Builders;
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
    public class CustomersControllerTests : IntegrationTestBase
    {
        public CustomersControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

        [Fact]
        public async Task GetAll_EmptyDatabase_ReturnsEmptyPagedResult()
        {
            ClearTable<Customer>();

            var response = await Client.GetAsync("/api/customers");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<PagedResponse<CustomerResponse>>();
            body!.Data.Should().BeEmpty();
            body.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetAll_WithSeededCustomers_ReturnsPaginatedList()
        {
            ClearTable<Customer>();
            foreach (var c in CustomerBuilder.BuildMany(3))
                await SeedAsync(c);

            var response = await Client.GetAsync("/api/customers?page=1&pageSize=10");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<PagedResponse<CustomerResponse>>();
            body!.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetAll_SearchFilter_ReturnsMatchingCustomers()
        {
            ClearTable<Customer>();
            await SeedAsync(new CustomerBuilder().WithFirstName("Alice")
                                                 .WithEmail("alice@example.com").Build());

            await SeedAsync(new CustomerBuilder().WithFirstName("Bob")
                                                 .WithEmail("bob@example.com").Build());

            var response = await Client.GetAsync("/api/customers?search=alice");

            var body = await response.Content.ReadFromJsonAsync<PagedResponse<CustomerResponse>>();
            body!.Data.Should().HaveCount(1);
            body.Data.First().FirstName.Should().Be("Alice");
        }

        [Fact]
        public async Task GetById_ExistingCustomer_ReturnsCustomer()
        {
            var customer = await SeedAsync(new CustomerBuilder().Build());

            var response = await Client.GetAsync($"/api/customers/{customer.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<CustomerResponse>();
            body!.Email.Should().Be(customer.Email);
        }

        [Fact]
        public async Task GetById_NonExistentId_Returns404()
        {
            var response = await Client.GetAsync($"/api/customers/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_ValidRequest_Returns201WithLocation()
        {
            var request = new CreateCustomerRequestBuilder()
                .WithEmail($"newuser_{Guid.NewGuid()}@example.com")
                .Build();

            var response = await Client.PostAsJsonAsync("/api/customers", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();

            var body = await response.Content.ReadFromJsonAsync<CustomerResponse>();
            body!.Email.Should().Be(request.Email.ToLower());
            body.FirstName.Should().Be(request.FirstName);
        }

        [Fact]
        public async Task Create_DuplicateEmail_Returns409()
        {
            var customer = await SeedAsync(new CustomerBuilder()
                .WithEmail("duplicate@example.com").Build());

            var request = new CreateCustomerRequestBuilder()
                .WithEmail("duplicate@example.com").Build();

            var response = await Client.PostAsJsonAsync("/api/customers", request);
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Create_InvalidRequest_Returns400WithErrors()
        {
            var request = new CreateCustomerRequestBuilder()
                .WithEmail("not-an-email")
                .WithFirstName("")
                .Build();

            var response = await Client.PostAsJsonAsync("/api/customers", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("errors");
        }

        [Fact]
        public async Task Update_ExistingCustomer_Returns200WithUpdatedData()
        {
            var customer = await SeedAsync(new CustomerBuilder().Build());
            var request = new UpdateCustomerRequest
            {
                FirstName = "updated",
                LastName = "Name",
                Phone = "+18095559876"
            };

            var response = await Client.PutAsJsonAsync($"/api/customers/{customer.Id}", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<CustomerResponse>();
            body!.FirstName.Should().Be("updated");
        }

        [Fact]
        public async Task Update_NonExistentCustomer_Returns404()
        {
            var request = new UpdateCustomerRequest { FirstName = "X", LastName = "Y" };
            var response = await Client.PutAsJsonAsync($"/api/customers/{Guid.NewGuid()}", request);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Deactivate_ActiveCustomer_Returns200()
        {
            var customer = await SeedAsync(new CustomerBuilder().Build());

            var response = await Client.PatchAsync(
                $"/api/customers/{customer.Id}/deactivate", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var getResponse = await Client.GetAsync($"/api/customers/{customer.Id}");
            var body = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            body!.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task Deactivate_AlreadyInactiveCustomer_Returns400()
        {
            var customer = await SeedAsync(new CustomerBuilder().AsInactive().Build());

            var response = await Client.PatchAsync($"/api/customers/{customer.Id}/deactivate", null);
            
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Delete_ExistingCustomer_Returns200AndRemovesRecord()
        {
            var customer = await SeedAsync(new CustomerBuilder()
                .WithEmail($"todelete_{Guid.NewGuid()}@example.com").Build());

            var response = await Client.DeleteAsync($"/api/customers/{customer.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var getResponse = await Client.GetAsync($"/api/customers/{customer.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
