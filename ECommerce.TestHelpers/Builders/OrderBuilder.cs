using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.TestHelpers.Builders
{
    public class OrderBuilder
    {
        private Guid _customerId = Guid.NewGuid();
        private OrderStatus _status = OrderStatus.Pending;
        private List<OrderItem> _items = new();
        private string _shippingJson = """{"street":"123 Main","city":"SD","zipCode":"10101","country":"DO"}""";

        public OrderBuilder WithCustomerId(Guid Id) { _customerId = Id; return this; }
        public OrderBuilder WithStatus(OrderStatus s) { _status = s; return this; }
        public OrderBuilder WithShippingJson(string j) { _shippingJson = j; return this; }
        public OrderBuilder WithItems(List<OrderItem> items) { _items = items; return this; }

        public Order Build() => new Order
        {
            CustomerId = _customerId,
            Status = _status,
            ShippingAddressJson = _shippingJson,
            Subtotal = _items.Sum(i => i.LineTotal),
            Tax = 0,
            Total = _items.Sum(i => i.LineTotal),
            Items = _items
        };
    }
}
