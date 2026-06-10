using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Domain
{
    public class OrderStateMachineTests
    {
        [Theory]
        [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Confirmed, OrderStatus.Processing)]
        [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Processing, OrderStatus.Packed)]
        [InlineData(OrderStatus.Processing, OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Packed, OrderStatus.Shipped)]
        [InlineData(OrderStatus.Packed, OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
        public void CanTransition_ValidTransitions_ReturnsTrue(OrderStatus from,  OrderStatus to)
        {
            OrderStateMachine.CanTransition(from, to).Should().BeTrue();
        }

        [Theory]
        [InlineData(OrderStatus.Pending, OrderStatus.Shipped)]
        [InlineData(OrderStatus.Pending, OrderStatus.Delivered)]
        [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Delivered, OrderStatus.Pending)]
        public void CanTransition_InvalidTransitions_ReturnsFalse(OrderStatus from, OrderStatus to)
        {
            OrderStateMachine.CanTransition(from, to).Should().BeFalse();
        }

        [Theory]
        [InlineData(OrderStatus.Delivered)]
        [InlineData(OrderStatus.Cancelled)]
        public void IsTerminal_TerminalStatuses_ReturnsTrue(OrderStatus status)
        {
            OrderStateMachine.IsTerminal(status).Should().BeTrue();
        }

        [Theory]
        [InlineData(OrderStatus.Pending)]
        [InlineData(OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Processing)]
        [InlineData(OrderStatus.Packed)]
        [InlineData(OrderStatus.Shipped)]
        public void IsTerminal_NonTerminalStatuses_ReturnsFalse(OrderStatus status)
        {
            OrderStateMachine.IsTerminal(status).Should().BeFalse();
        }

        [Fact]
        public void GetAllowedTransitions_Pending_ReturnsTwoOptions()
        {
            var allowed = OrderStateMachine.GetAllowedTransitions(OrderStatus.Pending);
            allowed.Should().BeEquivalentTo(new[]
            {
                OrderStatus.Confirmed,
                OrderStatus.Cancelled
            });
        }

        [Fact]
        public void GetAllowedTransitions_Terminal_ReturnsEmpty()
        {
            OrderStateMachine.GetAllowedTransitions(OrderStatus.Delivered).Should().BeEmpty();
            OrderStateMachine.GetAllowedTransitions(OrderStatus.Cancelled).Should().BeEmpty();
        }
    }
}
