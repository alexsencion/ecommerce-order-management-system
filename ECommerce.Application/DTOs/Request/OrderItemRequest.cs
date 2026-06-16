using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.DTOs.Request
{
    public class OrderItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
