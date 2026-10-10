using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class Shipment : BaseEntity
    {
        public Guid OrderId { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;
        public string? Notes { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        public Order Order { get; set; } = null!;

        public static Shipment Create(
            Guid orderId,
            string carrier,
            string trackingNumber,
            string? notes = null
            )
        {
            if (string.IsNullOrWhiteSpace(carrier))
                throw new ArgumentException("Carrier is required.", nameof(carrier));

            if (string.IsNullOrWhiteSpace(trackingNumber))
                throw new ArgumentException("Tracking number is required.", nameof(trackingNumber));

            return new Shipment
            {
                OrderId = orderId,
                Carrier = carrier.Trim(),
                TrackingNumber = trackingNumber.Trim(),
                Status = ShipmentStatus.Created,
                Notes = notes?.Trim(),
                ShippedAt = DateTime.UtcNow
            };
        }

        public void MarkDelivered()
        {
            if (Status == ShipmentStatus.Delivered)
                throw new InvalidOperationException(
                    "Shipment is already marked as delivered.");

            Status = ShipmentStatus.Delivered;
            DeliveredAt = DateTime.UtcNow;
        }
    }
}
