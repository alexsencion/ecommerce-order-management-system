using ECommerce.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public interface IStripeService
    {
        Task<StripePaymentIntentResult> CreatePaymentIntentAsync(Guid orderId, decimal amount, string currency = "usd");

        Task<StripePaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId);

        Task<bool> RefundPaymentAsync(string paymentIntentId);

        StripeWebhookResult? ParseWebhookEvent(string payload, string signature);
    }
}
