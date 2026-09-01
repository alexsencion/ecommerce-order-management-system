using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Common
{
    public class StripePaymentIntentResult
    {
        public string PaymentIntentId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsSucceeded => Status == "succeeded";
        public bool RequireAction => Status == "requires_action";
        public bool IsCancelled => Status == "canceled";
    }
}
