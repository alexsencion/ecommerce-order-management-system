using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid OrderId { get; set; }
        public string StripePaymentIntentId { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }

        public Order Order { get; set; } = null!;
    }
}
