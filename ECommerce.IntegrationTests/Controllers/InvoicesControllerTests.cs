using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Enums;
using ECommerce.IntegrationTests.Common;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.IntegrationTests.Controllers
{
    public class InvoicesControllerTests : IntegrationTestBase
    {
        public InvoicesControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<InvoiceResponse> GenerateFor(Guid orderId)
        {
            var response = await Client.PostAsync($"/api/invoices/order/{orderId}", null);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await response.Content.ReadFromJsonAsync<InvoiceResponse>())!;
        }

        [Fact]
        public async Task Generate_PaidOrder_Returns201AndStoresRealPdf()
        {
            var (customer, _, order) = await SeedOrderAsync(status: OrderStatus.Delivered);

            var invoice = await GenerateFor(order.Id);

            invoice.OrderId.Should().Be(order.Id);
            invoice.InvoiceNumber.Should().MatchRegex(@"^INV-\d{6}-\d{5}$");
            invoice.BillToEmail.Should().Be(customer.Email);
            invoice.Total.Should().Be(order.Total);

            var stored = Storage.Files
                .Single(f => f.Key.EndsWith($"{invoice.InvoiceNumber}.pdf"));
            Encoding.ASCII.GetString(stored.Value, 0, 4).Should().Be("%PDF");
        }

        [Fact]
        public async Task Generate_CallledTwice_ReturnsSameInvoiceWith200()
        {
            var (_, _, order) = await SeedOrderAsync(status: OrderStatus.Shipped);
            var first = await GenerateFor(order.Id);

            var second = await Client.PostAsync($"/api/invoices/order/{order.Id}", null);

            second.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await second.Content.ReadFromJsonAsync<InvoiceResponse>();
            body!.Id.Should().Be(first.Id);

            DbContext.Invoices.Count(i => i.OrderId == order.Id).Should().Be(1);
        }

        [Theory]
        [InlineData(OrderStatus.Pending)]
        [InlineData(OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Cancelled)]
        public async Task Generate_UnpaidOrder_Returns400(OrderStatus status)
        {
            var (_, _, order) = await SeedOrderAsync(status: status);

            var response = await Client.PostAsync($"/api/invoices/order/{order.Id}", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            DbContext.Invoices.Any(i => i.OrderId == order.Id).Should().BeFalse();
        }

        [Fact]
        public async Task Generate_NonExistentOrder_Returns404()
        {
            var response = await Client.PostAsync($"/api/invoices/order/{Guid.NewGuid()}", null);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetByOrder_AfterGeneration_Returns200()
        {
            var (_, _, order) = await SeedOrderAsync(status: OrderStatus.Processing);
            var invoice = await GenerateFor(order.Id);

            var response = await Client.GetAsync($"api/invoices/order/{order.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
            body!.InvoiceNumber.Should().Be(invoice.InvoiceNumber);
        }

        [Fact]
        public async Task GetByOrder_NoInvoiceYet_Returns404()
        {
            var (_, _, order) = await SeedOrderAsync(status: OrderStatus.Delivered);

            var response = await Client.GetAsync($"/api/invoices/order/{order.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Download_ExistingInvoice_ReturnsPdfAttachment()
        {
            var (_, _, order) = await SeedOrderAsync(status: OrderStatus.Delivered);
            var invoice = await GenerateFor(order.Id);

            var response = await Client.GetAsync($"/api/invoices/{invoice.Id}/download");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
            response.Content.Headers.ContentDisposition!.ToString()
                .Should().Contain(invoice.FileName);

            var bytes = await response.Content.ReadAsByteArrayAsync();
            Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
        }

        [Fact]
        public async Task Download_NonExistentInvoice_Returns404()
        {
            var response = await Client.GetAsync($"/api/invoices/{Guid.NewGuid()}/download");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
