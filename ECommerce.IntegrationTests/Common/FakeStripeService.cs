using ECommerce.Application.Common;
using ECommerce.Application.Services;
using Stripe.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.IntegrationTests.Common
{
    public class FakeStripeService : IStripeService
    {
        public static string NextPaymentIntentId { get; set; } = "pi_fake_default";
        public static string NextClientSecret { get; set; } = "pi_fake_default_secret";
        public static string NextStatus { get; set; } = "requires_payment_method";
        public static bool RefundShouldSucceed { get; set; } = true;
        public static StripeWebhookResult? NextWebhookResult { get; set; }


        public Task<StripePaymentIntentResult> CreatePaymentIntentAsync(Guid orderId, decimal amount, string currency = "usd")
        {
            return Task.FromResult(new StripePaymentIntentResult
            {
                PaymentIntentId = NextPaymentIntentId,
                ClientSecret = NextClientSecret,
                Status = NextStatus,
                Amount = amount
            });
        }

        public Task<StripePaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId)
        {
            return Task.FromResult(new StripePaymentIntentResult
            {
                PaymentIntentId = paymentIntentId,
                ClientSecret = NextClientSecret,
                Status = NextStatus,
                Amount = 0
            });
        }

        public StripeWebhookResult? ParseWebhookEvent(string payload, string signature) => NextWebhookResult;

        public Task<bool> RefundPaymentAsync(string paymentIntentId) => Task.FromResult(RefundShouldSucceed);

        public static void Reset()
        {
            NextPaymentIntentId = "pi_fake_default";
            NextClientSecret = "pi_fake_default_secret";
            NextStatus = "requires_payment_method";
            RefundShouldSucceed = true;
            NextWebhookResult = null;
        }
    }
}
