using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Common
{
    public class StripeWebhookResult
    {
        public string EventType { get; set; } = string.Empty;
        public string PaymentIntentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        public const string PaymentSucceeded = "payment_intent.succeeded";
        public const string PaymentFailed = "payment_intent.payment_failed";
        public const string PaymentCancelled = "payment_intent.canceled";
    }
}
