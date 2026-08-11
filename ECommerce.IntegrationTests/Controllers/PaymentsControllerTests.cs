using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.IntegrationTests.Controllers
{
    public class PaymentsControllerTests : IntegrationTestBase
    {
        public PaymentsControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            FakeStripeService.Reset();
        }

        private async Task<Order> SeedConfirmedOrderAsync()
        {
            var (_, _, order) = await SeedOrderAsync(status: OrderStatus.Confirmed);
            return order;
        }

        [Fact]
        public async Task CreateIntent_ConfirmedOrder_Returns200WithClientSecret()
        {
            var order = await SeedConfirmedOrderAsync();
            FakeStripeService.NextPaymentIntentId = "pi_integration_test_1";
            FakeStripeService.NextClientSecret = "pi_integration_test_1_secret";

            var response = await Client.PostAsJsonAsync("/api/payments/intent",
                new CreatePaymentIntentRequest { OrderId = order.Id });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<PaymentIntentResponse>();
            body!.ClientSecret.Should().Be("pi_integration_test_1_secret");
            body!.PaymentIntentId.Should().Be("pi_integration_test_1");

            var payment = DbContext.Payments.FirstOrDefault(p => p.OrderId == order.Id);
            payment.Should().NotBeNull();
            payment!.Status.Should().Be(PaymentStatus.Pending);
        }

        [Fact]
        public async Task CreateIntent_PendingOrder_Returns400()
        {
            var (_, _, order) = await SeedOrderAsync(status: OrderStatus.Pending);

            var response = await Client.PostAsJsonAsync("/api/payments/intent", 
                new CreatePaymentIntentRequest { OrderId= order.Id });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateIntent_NonExistentOrder_Returns404()
        {
            var response = await Client.PostAsJsonAsync("/api/payments/intent",
                new CreatePaymentIntentRequest { OrderId = Guid.NewGuid() });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateIntent_CalledTwice_ReusesSameIntent()
        {
            var order = await SeedConfirmedOrderAsync();
            FakeStripeService.NextPaymentIntentId = "pi_reused_intent";
            FakeStripeService.NextClientSecret = "pi_reused_intent_secret";

            var first = await Client.PostAsJsonAsync("/api/payments/intent",
                new CreatePaymentIntentRequest { OrderId = order.Id });

            var second = await Client.PostAsJsonAsync("/api/payments/intent",
                new CreatePaymentIntentRequest { OrderId = order.Id });

            first.StatusCode.Should().Be(HttpStatusCode.OK);
            second.StatusCode.Should().Be(HttpStatusCode.OK);

            var firstBody = await first.Content.ReadFromJsonAsync<PaymentIntentResponse>();
            var secondBody = await second.Content.ReadFromJsonAsync<PaymentIntentResponse>();
            firstBody!.PaymentIntentId.Should().Be(secondBody!.PaymentIntentId);

            var count = DbContext.Payments.Count(p => p.OrderId == order.Id);
            count.Should().Be(1);
        }

        [Fact]
        public async Task GetByOrder_ExistingPayment_Returns200()
        {
            var order = await SeedConfirmedOrderAsync();
            var payment = new Payment
            {
                OrderId = order.Id,
                StripePaymentIntentId = "pi_existing",
                Status = PaymentStatus.Pending,
                Amount = order.Total,
            };
            DbContext.Payments.Add(payment);
            await DbContext.SaveChangesAsync();

            var response = await Client.GetAsync($"/api/payments/order/{order.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<PaymentResponse>();
            body!.OrderId.Should().Be(order.Id);
        }

        [Fact]
        public async Task GetByOrder_NoPayment_Returns404()
        {
            var order = await SeedConfirmedOrderAsync();

            var response = await Client.GetAsync($"/api/payments/order/{order.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Refund_SucceededPayment_Returns200AndCancelsOrder()
        {
            var order = await SeedConfirmedOrderAsync();
            var payment = new Payment
            {
                OrderId = order.Id,
                StripePaymentIntentId = "pi_to_refund",
                Status = PaymentStatus.Succeeded,
                Amount = order.Total,
                PaidAt = DateTime.UtcNow,
            };
            DbContext.Payments.Add(payment);
            await DbContext.SaveChangesAsync();

            FakeStripeService.RefundShouldSucceed = true;

            var response = await Client.PostAsync(
                $"/api/payments/order/{order.Id}/refund", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedOrder = await DbContext.Orders
                .AsNoTracking()
                .SingleAsync(o => o.Id == order.Id);

            updatedOrder.Status.Should().Be(OrderStatus.Cancelled);

            var updatedPayment = await DbContext.Payments
                .AsNoTracking()
                .SingleAsync(p => p.Id == payment.Id);

            updatedPayment.Status.Should().Be(PaymentStatus.Refunded);
        }

        [Fact]
        public async Task Refund_UnpaidOrder_Returns404()
        {
            var order = await SeedConfirmedOrderAsync();

            var response = await Client.PostAsync(
                $"/api/payments/order/{order.Id}/refund", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Refund_StripeFailure_Returns400AndDoesNotCancelOrder()
        {
            var order = await SeedConfirmedOrderAsync();
            var payment = new Payment
            {
                OrderId = order.Id,
                StripePaymentIntentId = "pi_refund_fails",
                Status = PaymentStatus.Succeeded,
                Amount = order.Total,
                PaidAt = DateTime.UtcNow,
            };
            DbContext.Payments.Add(payment);
            await DbContext.SaveChangesAsync();

            FakeStripeService.RefundShouldSucceed = false;

            var response = await Client.PostAsync(
                $"/api/payments/order/{order.Id}/refund", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var updatedOrder = await DbContext.Orders
               .AsNoTracking()
               .SingleAsync(o => o.Id == order.Id);

            updatedOrder.Status.Should().Be(OrderStatus.Confirmed);
        }

        [Fact]
        public async Task Webhook_PaymentSucceededEvent_AdvancesOrderToProcessing()
        {
            var order = await SeedConfirmedOrderAsync();
            var payment = new Payment
            {
                OrderId = order.Id,
                StripePaymentIntentId = "pi_webhook_success",
                Status = PaymentStatus.Pending,
                Amount = order.Total
            };
            DbContext.Payments.Add(payment);
            await DbContext.SaveChangesAsync();

            FakeStripeService.NextWebhookResult = new StripeWebhookResult
            {
                EventType = StripeWebhookResult.PaymentSucceeded,
                PaymentIntentId = "pi_webhook_success",
                Status = "succeeded",
                Amount = order.Total
            };

            var content = new StringContent(
                "{}", Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, "api/payments/webhook")
            {
                Content = content
            };
            request.Headers.Add("Stripe-Signature", "fake_signature_for_test");

            var response = await Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedPayment = await DbContext.Payments
                .AsNoTracking()
                .SingleAsync(p => p.Id == payment.Id);

            updatedPayment.Status.Should().Be(PaymentStatus.Succeeded);

            var updatedOrder = await DbContext.Orders
                .AsNoTracking()
                .SingleAsync(o => o.Id == order.Id);

            updatedOrder.Status.Should().Be(OrderStatus.Processing);


        }

        [Fact]
        public async Task Webhook_MissingSignatureHeader_Returns400()
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await Client.PostAsync("/api/payments/webhook", content);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Webhook_InvalidSignature_Returns400()
        {
            FakeStripeService.NextWebhookResult = null;

            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhook")
            {
                Content = content
            };
            request.Headers.Add("Stripe-Signature", "bad_signature");

            var response = await Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Webhook_PaymentFailedEvent_MarksPaymentFailedWithoutCancellingOrder()
        {
            var order = await SeedConfirmedOrderAsync();
            var payment = new Payment
            {
                OrderId = order.Id,
                StripePaymentIntentId = "pi_webhook_fail",
                Status = PaymentStatus.Pending,
                Amount = order.Total
            };
            DbContext.Payments.Add(payment);
            await DbContext.SaveChangesAsync();

            FakeStripeService.NextWebhookResult = new StripeWebhookResult
            {
                EventType = StripeWebhookResult.PaymentFailed,
                PaymentIntentId = "pi_webhook_fail"
            };

            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhook")
            {
                Content = content
            };
            request.Headers.Add("Stripe-Signature", "fake_sig");

            var response = await Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedPayment = await DbContext.Payments
                .AsNoTracking()
                .SingleAsync(p => p.Id == payment.Id);

            updatedPayment.Status.Should().Be(PaymentStatus.Failed);
        }
    }
}
