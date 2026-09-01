using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.TestHelpers.Builders
{
    public class PaymentBuilder
    {
        private Guid _orderId = Guid.NewGuid();
        private string _intentId = "pi_test_123456";
        private PaymentStatus _status = PaymentStatus.Pending;
        private decimal _amount = 118m;
        private DateTime? _paidAt = null;

        public PaymentBuilder WithOrderId(Guid id) { _orderId = id; return this; }
        public PaymentBuilder WithIntentId(string id) { _intentId = id; return this; }

        public PaymentBuilder WithStatus(PaymentStatus s) { _status = s; return this; }

        public PaymentBuilder WithAmount(decimal a) { _amount = a; return this; }
        public PaymentBuilder AsSucceeded()
        {
            _status = PaymentStatus.Succeeded;
            _paidAt = DateTime.UtcNow;
            return this;
        }

        public Payment Build() => new()
        {
            OrderId = _orderId,
            StripePaymentIntentId = _intentId,
            Status = _status,
            Amount = _amount,
            PaidAt = _paidAt
        };
    }
}
