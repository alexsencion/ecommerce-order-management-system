using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.DTOs.Response
{
    public class OrderStatusHistoryResponse
    {
        public OrderStatus FromStatus { get; set; }
        public OrderStatus ToStatus { get; set; }
        public string? Notes { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
