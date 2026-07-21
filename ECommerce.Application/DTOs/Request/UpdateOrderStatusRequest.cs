using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.DTOs.Request
{
    public class UpdateOrderStatusRequest
    {
        public OrderStatus NewStatus { get; set; }
        public string? Notes { get; set; }
    }
}
