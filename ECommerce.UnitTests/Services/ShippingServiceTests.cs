using AutoMapper;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.Services;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces.Repositories;
using ECommerce.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Services
{
    public class ShippingServiceTests : BaseTest
    {
        private readonly Mock<IGenericRepository<Shipment>> _shipmentRepo = new();
        private readonly Mock<IGenericRepository<OrderStatusHistory>> _historyRepo = new();
        
        private readonly ShippingService _sut;

        public ShippingServiceTests() : base()
        {
            _uowMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
            _uowMock.Setup(u => u.Repository<Shipment>()).Returns(_shipmentRepo.Object);
            _uowMock.Setup(u => u.Repository<OrderStatusHistory>())
                    .Returns(_historyRepo.Object);
            _uowMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _sut = new ShippingService(
                _uowMock.Object,
                _mapper,
                _loggerShipmentMock.Object);
        }

        private Order PackedOrder(Guid? orderId = null)
        {
            var order = new OrderBuilder()
                .WithStatus(OrderStatus.Packed)
                .WithItems(new List<OrderItem>())
                .Build();
            order.Customer = new CustomerBuilder().Build();

            if (orderId.HasValue)
            {
                typeof(BaseEntity)
                    .GetProperty("Id")!
                    .SetValue(order, orderId.Value);
            }
            return order;
        }

        private CreateShipmentRequest ValidRequest(Guid orderId) => new()
        {
            OrderId = orderId,
            Carrier = "FedEx",
            TrackingNumber = "FEDEX-123456",
            Notes = "Fragile"
        };

        [Fact]
        public async Task CreateShipment_PackedOrder_CreatesShipmentAndTransitionsToShipped()
        {
            var order = PackedOrder();
            var request = ValidRequest(order.Id);

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);
            _shipmentRepo.Setup(r => r.AddAsync(It.IsAny<Shipment>()))
                .Returns(Task.CompletedTask);

            var result = await _sut.CreateShipmentAsync(request);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Value!.Carrier.Should().Be("FedEx");
            result.Value!.TrackingNumber.Should().Be("FEDEX-123456");
            result.Value.Status.Should().Be(ShipmentStatus.Created);

            order.Status.Should().Be(OrderStatus.Shipped);
            _uowMock.Verify(u => u.CommitTransactionAsync(), Times.Once());
        }

        [Fact]
        public async Task CreateShipment_OrderNotPacked_ReturnsFailure()
        {
            var order = new OrderBuilder()
                .WithStatus(OrderStatus.Confirmed)
                .Build();

            order.Customer = new CustomerBuilder().Build();

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);

            var result = await _sut.CreateShipmentAsync(ValidRequest(order.Id));

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Error.Should().Contain("packed");
            _uowMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateShipment_DuplicateShipment_ReturnsConflict()
        {
            var order = PackedOrder();

            order.Shipment = new ShipmentBuilder()
                .WithOrderId(order.Id).Build();

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);

            var result = await _sut.CreateShipmentAsync(ValidRequest(order.Id));

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            _uowMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateShipment_OrderNotFound_Returns404()
        {
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>()))
                           .ReturnsAsync((Order?)null);

            var result = await _sut.CreateShipmentAsync(ValidRequest(Guid.NewGuid()));

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetByOrder_ExistingShipment_ReturnsShipment()
        {
            var orderId = Guid.NewGuid();
            var shipment = new ShipmentBuilder().WithOrderId(orderId).Build();
            var order = new OrderBuilder()
                .WithStatus(OrderStatus.Shipped)
                .Build();
            order.Customer = new CustomerBuilder().Build();
            order.Shipment = shipment;

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(orderId)).ReturnsAsync(order);

            var result = await _sut.GetByOrderAsync(orderId);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Carrier.Should().Be("FedEx");
        }

        [Fact]
        public async Task GetByOrder_NoShipment_Returns404()
        {
            var order = new OrderBuilder()
                .WithStatus(OrderStatus.Packed)
                .Build();
            order.Customer = new CustomerBuilder().Build();
            order.Shipment = null;

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);

            var result = await _sut.GetByOrderAsync(order.Id);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateShipment_ValidRequest_UpdatesCarrierAndTracking()
        {
            var shipment = new ShipmentBuilder()
                .WithCarrier("FedEx").WithTracking("FEDEX-OLD").Build();

            _shipmentRepo.Setup(r => r.GetByIdAsync(shipment.Id)).ReturnsAsync(shipment);

            var result = await _sut.UpdateShipmentAsync(shipment.Id, new UpdateShipmentRequest
            {
                Carrier = "UPS",
                TrackingNumber = "UPS-NEW123",
                Notes = "Updated notes"
            });

            result.IsSuccess.Should().BeTrue();
            result.Value!.Carrier.Should().Be("UPS");
            result.Value.TrackingNumber.Should().Be("UPS-NEW123");
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once());
        }

        [Fact]
        public async Task UpdateShipment_DeliveredShipment_ReturnsFailure()
        {
            var shipment = new ShipmentBuilder().AsDelivered().Build();
            _shipmentRepo.Setup(r => r.GetByIdAsync(shipment.Id)).ReturnsAsync(shipment);

            var result = await _sut.UpdateShipmentAsync(shipment.Id, new UpdateShipmentRequest
            {
                Carrier = "DHL",
                TrackingNumber = "DHL-123"
            });

            result.IsSuccess!.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateShipment_NotFound_Returns404()
        {
            _shipmentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                         .ReturnsAsync((Shipment?)null);

            var result = await _sut.UpdateShipmentAsync(Guid.NewGuid(),
                new UpdateShipmentRequest { Carrier = "UPS", TrackingNumber = "UPS-001" });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task MarkDelivered_ShippedOrder_TransitionsToDeliveredAndSetsTimestamp()
        {
            var orderId = Guid.NewGuid();
            var shipment = new ShipmentBuilder().WithOrderId(orderId).Build();
            var order = new OrderBuilder()
                .WithStatus(OrderStatus.Shipped)
                .WithItems(new List<OrderItem>())
                .Build();
            order.Customer = new CustomerBuilder().Build();

            _shipmentRepo.Setup(r => r.GetByIdAsync(shipment.Id)).ReturnsAsync(shipment);
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(orderId)).ReturnsAsync(order);

            var result = await _sut.MarkDeliveredAsync(shipment.Id);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Status.Should().Be(ShipmentStatus.Delivered);
            result.Value.DeliveredAt.Should().NotBeNull();

            order.Status.Should().Be(OrderStatus.Delivered);
            _uowMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
           
        }

        [Fact]
        public async Task MarkDelivered_AlreadyDelivered_ReturnsFailure()
        {
            var shipment = new ShipmentBuilder().AsDelivered().Build();
            _shipmentRepo.Setup(r => r.GetByIdAsync(shipment.Id)).ReturnsAsync(shipment);

            var result = await _sut.MarkDeliveredAsync(shipment.Id);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            _uowMock.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task MarkDelivered_NotFound_Returns404()
        {
            _shipmentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                         .ReturnsAsync((Shipment?)null);

            var result = await _sut.MarkDeliveredAsync(Guid.NewGuid());

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }
    }
}
