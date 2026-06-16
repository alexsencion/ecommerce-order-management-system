using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string ShippingAddressJson { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public Customer Customer { get; set; } = null!;
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
        public Payment? Payment { get; set; }
        public Shipment? Shipment { get; set; }

        public OrderStatusHistory Transition(OrderStatus newStatus, string? notes = null)
        {
            if (!OrderStateMachine.CanTransition(Status, newStatus))
                throw new InvalidOperationException(
                    $"Cannot transition order from {Status} to {newStatus}.");

            var history = new OrderStatusHistory
            {
                OrderId = Id,
                FromStatus = Status,
                ToStatus = newStatus,
                Notes = notes,
                ChangedAt = DateTime.UtcNow
            };

            Status = newStatus;
            return history;
        }
    }
}
