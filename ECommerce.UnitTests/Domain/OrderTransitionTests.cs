using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Domain
{
    public class OrderTransitionTests
    {
        private Order BuildOrder(OrderStatus status = OrderStatus.Pending) => new()
        {
            CustomerId = Guid.NewGuid(),
            ShippingAddressJson = "{}",
            Status = status
        };

        [Fact]
        public void Transition_ValidMove_UpdatesStatusAndReturnsHistory()
        {
            var order = BuildOrder(OrderStatus.Pending);

            var history = order.Transition(OrderStatus.Confirmed, "Payment received");

            order.Status.Should().Be(OrderStatus.Confirmed);
            history.FromStatus.Should().Be(OrderStatus.Pending);
            history.ToStatus.Should().Be(OrderStatus.Confirmed);
            history.Notes.Should().Be("Payment received");
            history.ChangeAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Transition_InvalidMove_Throws()
        {
            var order = BuildOrder(OrderStatus.Delivered);

            var act = () => order.Transition(OrderStatus.Cancelled);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot transition*");
        }

        [Fact]
        public void Transition_FullHappyPath_CompletesWithoutThrowing()
        {
            var order = BuildOrder(OrderStatus.Pending);

            order.Transition(OrderStatus.Confirmed);
            order.Transition(OrderStatus.Processing);
            order.Transition(OrderStatus.Packed);
            order.Transition(OrderStatus.Shipped);
            order.Transition(OrderStatus.Delivered);

            order.Status.Should().Be(OrderStatus.Delivered);
        }

        [Fact]
        public void Transition_CancelFromPending_Succeeds()
        {
            var order = BuildOrder(OrderStatus.Pending);
            var act = () => order.Transition(OrderStatus.Cancelled);
            act.Should().NotThrow();
            order.Status.Should().Be(OrderStatus.Cancelled);
        }
    }
}
