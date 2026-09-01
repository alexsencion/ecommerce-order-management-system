using ECommerce.Application.Common;
using ECommerce.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Services
{
    public class StripeService : IStripeService
    {
        private readonly StripeSettings _settings;
        private readonly ILogger<StripeService> _logger;

        public StripeService(IOptions<StripeSettings> settings, ILogger<StripeService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            StripeConfiguration.ApiKey = _settings.SecretKey;
        }

        public async Task<StripePaymentIntentResult> CreatePaymentIntentAsync(Guid orderId, decimal amount, string currency = "usd")
        {
            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100),
                    Currency = currency,
                    Metadata = new Dictionary<string, string>
                    {
                        ["orderId"] = orderId.ToString()
                    },
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"
                    }
                };

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);

                _logger.LogInformation(
                    "PaymentIntent created: {PaymentIntentId} for order {OrderId}",
                    intent.Id, orderId);

                return MapToResult(intent);
            }
            catch (StripeException  ex)
            {
                _logger.LogError(ex,
                    "Stripe error creating PaymentIntent for order {OrderId}: {Message}", orderId, ex.Message);
                throw new InvalidOperationException($"Failed to create payment: {ex.StripeError?.Message ?? ex.Message}");
            }
        }

        public async Task<StripePaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                var intent = await service.GetAsync(paymentIntentId);
                return MapToResult(intent);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex,
                    "Stripe error retrieving PaymentIntent {PaymentIntentId}: {Message}", paymentIntentId, ex.Message);
                throw new InvalidOperationException(
                    $"Failed to retrieve payment status: {ex.StripeError?.Message ?? ex.Message}");
            }
        }

        public StripeWebhookResult? ParseWebhookEvent(string payload, string signature)
        {
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(payload, signature, _settings.WebhookSecret);

                var result = new StripeWebhookResult
                {
                    EventType = stripeEvent.Type
                };

                switch (stripeEvent.Data.Object)
                {
                    case PaymentIntent intent:
                        result.PaymentIntentId = intent.Id;
                        result.Status = intent.Status;
                        result.Amount = intent.Amount / 100m;
                        break;

                    case Charge charge:
                        result.PaymentIntentId = charge.PaymentIntentId;
                        break;

                    case Refund refund:
                        break;
                }
                return result;

            }
            catch (StripeException ex)
            {
                _logger.LogWarning(
                    "Invalid Stripe webhook signature: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<bool> RefundPaymentAsync(string paymentIntentId)
        {
            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                };

                var service = new RefundService();
                var refund = await service.CreateAsync(options);

                _logger.LogInformation(
                    "Refund issued for PaymentIntent {PaymentIntentId}. RefundId: {RefundId}",
                    paymentIntentId, refund.Id);

                return refund.Status == "succeeded";
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex,
                    "Stripe error refunding PaymentIntent {PaymentIntentId}: {Message}", paymentIntentId, ex.Message);
                return false;
            }
        }

        private static StripePaymentIntentResult MapToResult(PaymentIntent intent) => new()
        {
            PaymentIntentId = intent.Id,
            ClientSecret = intent.ClientSecret,
            Status = intent.Status,
            Amount = intent.Amount / 100m
        };
    }
}
