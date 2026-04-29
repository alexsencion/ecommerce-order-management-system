using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class OrderStatusHistory : BaseEntity
    {
        public Guid OrderId { get; set; }
        public OrderStatus FromStatus{ get; set; }
        public OrderStatus ToStatus { get; set; }
        public string? Notes { get; set; }
        public DateTime ChangeAt { get; set; } = DateTime.UtcNow;

        public Order Order { get; set; } = null!;
    }
}
