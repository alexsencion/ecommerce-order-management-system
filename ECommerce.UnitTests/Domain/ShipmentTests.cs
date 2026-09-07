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
    public class ShipmentTests
    {
        [Fact]
        public void Create_ValidArguments_ReturnsShipmentInCreatedStatus()
        {
            var shipment = Shipment.Create(
                Guid.NewGuid(), "FedEx", "TRACK123", "Handle with care");

            shipment.Carrier.Should().Be("FedEx");
            shipment.TrackingNumber.Should().Be("TRACK123");
            shipment.Notes.Should().Be("Handle with care");
            shipment.Status.Should().Be(ShipmentStatus.Created);
            shipment.ShippedAt.Should().NotBeNull();
            shipment.DeliveredAt.Should().BeNull();
        }

        [Theory]
        [InlineData("", "TRACK123")]
        [InlineData("  ", "TRACK123")]
        public void Create_EmptyCarrier_Throws(string carrier, string tracking)
        {
           var act = () => Shipment.Create(Guid.NewGuid(), carrier, tracking);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Carrier*");
        }

        [Theory]
        [InlineData("FedEx", "")]
        [InlineData("FedEx", "  ")]
        public void Create_EmptyTrackingNumber_Throws(string carrier, string tracking)
        {
            var act = () => Shipment.Create(Guid.NewGuid(), carrier, tracking);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Tracking number*");
        }

        [Fact]
        public void Create_TrimsCarrierAndTrackingNumber()
        {
            var shipment = Shipment.Create(
                Guid.NewGuid(), "  UPS  ", "  1Z999AA10123456784  ");

            shipment.Carrier.Should().Be("UPS");
            shipment.TrackingNumber.Should().Be("1Z999AA10123456784");
        }

        [Fact]
        public void MarkDelivered_TransitionsToDeliveredAndSetsTimestamp()
        {
            var shipment = Shipment.Create(Guid.NewGuid(), "DHL", "DHL12345");

            shipment.MarkDelivered();

            shipment.Status.Should().Be(ShipmentStatus.Delivered);
            shipment.DeliveredAt.Should().NotBeNull();
            shipment.DeliveredAt.Should().BeCloseTo(
                DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void MarkDelivered_AlreadyDelivered_Throws()
        {
            var shipment = Shipment.Create(Guid.NewGuid(), "DHL", "DHL12345");
            shipment.MarkDelivered();

            var act = () => shipment.MarkDelivered();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*already marked as delivered*");
        }
    }
}
