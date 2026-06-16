using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.DTOs.Request
{
    public class CreateOrderRequest
    {
        public Guid CustomerId { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
        public ShippingAddressRequest ShippingAddress { get; set; } = new();
        public string? Notes { get; set; }
    }
}
