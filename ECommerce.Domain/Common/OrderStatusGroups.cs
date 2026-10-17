using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Common
{
    public static class OrderStatusGroups
    {
        public static readonly IReadOnlyList<OrderStatus> Paid = new[]
        {
            OrderStatus.Processing,
            OrderStatus.Packed,
            OrderStatus.Shipped,
            OrderStatus.Delivered
        };

        public static bool IsPaid(OrderStatus status) => Paid.Contains(status);
    }
}
