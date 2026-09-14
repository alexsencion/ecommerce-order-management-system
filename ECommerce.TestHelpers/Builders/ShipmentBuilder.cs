using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.TestHelpers.Builders
{
    public class ShipmentBuilder
    {
        private Guid _orderId = Guid.NewGuid();
        private string _carrier = "FedEx";
        private string _tracking = "FEDEX123456";
        private string? _notes = null;
        private ShipmentStatus _status = ShipmentStatus.Created;

        public ShipmentBuilder WithOrderId(Guid id) { _orderId = id; return this; }
        public ShipmentBuilder WithCarrier(string c) { _carrier = c; return this; }
        public ShipmentBuilder WithTracking(string t) { _tracking = t; return this; }
        public ShipmentBuilder WithNotes(string? n) { _notes = n; return this; }
        public ShipmentBuilder AsDelivered()
        {
            _status = ShipmentStatus.Delivered;
            return this;
        }

        public Shipment Build()
        {
            var s = Shipment.Create(_orderId, _carrier, _tracking, _notes);
            if (_status == ShipmentStatus.Delivered)
                s.MarkDelivered();
            return s;
        }
    }
}
