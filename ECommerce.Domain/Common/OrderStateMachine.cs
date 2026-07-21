using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Common
{
    public static class OrderStateMachine
    {
        private static readonly Dictionary<OrderStatus, IReadOnlySet<OrderStatus>> _allowedTransitions = new()
        {
            [OrderStatus.Pending] = new HashSet<OrderStatus>() { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new HashSet<OrderStatus>() { OrderStatus.Processing, OrderStatus.Cancelled },
            [OrderStatus.Processing] = new HashSet<OrderStatus>() { OrderStatus.Packed, OrderStatus.Cancelled },
            [OrderStatus.Packed] = new HashSet<OrderStatus>() { OrderStatus.Shipped, OrderStatus.Cancelled },
            [OrderStatus.Shipped] = new HashSet<OrderStatus>() { OrderStatus.Delivered },
            [OrderStatus.Delivered] = new HashSet<OrderStatus>(),
            [OrderStatus.Cancelled] = new HashSet<OrderStatus>(),
        };

        public static bool CanTransition(OrderStatus from, OrderStatus to) => 
            _allowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

        public static IReadOnlySet<OrderStatus> GetAllowedTransitions(OrderStatus from) =>
            _allowedTransitions.TryGetValue(from, out var allowed)
                ? allowed
                : new HashSet<OrderStatus>();

        public static bool IsTerminal(OrderStatus status) =>
            status is OrderStatus.Delivered or OrderStatus.Cancelled;
    }
}
