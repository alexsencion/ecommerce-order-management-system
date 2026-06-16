using AutoMapper;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.Mappings;
using ECommerce.Application.Services;
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
    public class OrderServiceTests : BaseTest
    {
        private readonly Mock<IOrderRepository> _orderRepoMock = new();
        private readonly Mock<ICustomerRepository> _customerRepoMock = new();
        private readonly Mock<IProductRepository> _productRepoMock = new();

        private readonly OrderService _sut;

        public OrderServiceTests() : base()
        {
            _uowMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
            _uowMock.Setup(u => u.Customers).Returns(_customerRepoMock.Object);
            _uowMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

            _sut = new OrderService(_uowMock.Object, _mapper, _loggerOrderMock.Object);
        }

        private CreateOrderRequest ValidCreateRequest(Guid customerId, Guid productId) => new()
        {
            CustomerId = customerId,
            Items = new() { new() { ProductId = productId, Quantity = 2 } },
            ShippingAddress = new()
            {
                Street = "123 Main St",
                City = "Santo Domingo",
                State = "DN",
                ZipCode = "10101",
                Country = "DO"
            }
        };

        [Fact]
        public async Task GetByIdAsync_ExistingOrder_ReturnsSuccess()
        {
            var order = new OrderBuilder().Build();
            order.Customer = new CustomerBuilder().Build();
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);

            var result = await _sut.GetByIdAsync(order.Id);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_Returns404()
        {
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>()))
                          .ReturnsAsync((Order?)null);

            var result = await _sut.GetByIdAsync(Guid.NewGuid());

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_CreatesOrderAndReservesStock()
        {
            var customer = new CustomerBuilder().Build();
            var product = new ProductBuilder().WithStock(100).WithPrice(50m).Build();
            var request = ValidCreateRequest(customer.Id, product.Id);

            _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id)).ReturnsAsync(customer);
            _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
            _orderRepoMock.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>()))
                          .ReturnsAsync((Guid id) =>
                          {
                              var o = new OrderBuilder()
                                  .WithCustomerId(customer.Id).Build();
                              o.Customer = customer;
                              return o;
                          });

            var result = await _sut.CreateAsync(request);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);

            product.ReservedQuantity.Should().Be(2);
            _productRepoMock.Verify(r => r.Update(product), Times.Once);
            _uowMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_CustomerNotFound_Returns404()
        {
            _customerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                             .ReturnsAsync((Customer?)null);

            var result = await _sut.CreateAsync(
                ValidCreateRequest(Guid.NewGuid(), Guid.NewGuid()));

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            _uowMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_InactiveCustomer_ReturnsFailure()
        {
            var customer = new CustomerBuilder().AsInactive().Build();
            _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id)).ReturnsAsync(customer);

            var result = await _sut.CreateAsync(
                ValidCreateRequest(customer.Id, Guid.NewGuid()));

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreateAsync_InsufficientStock_RollsBackAndReturns422()
        {
            var customer = new CustomerBuilder().Build();
            var product = new ProductBuilder().WithStock(1).Build();

            _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id)).ReturnsAsync(customer);
            _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

            var request = ValidCreateRequest(customer.Id, product.Id);
            request.Items[0].Quantity = 5;

            var result = await _sut.CreateAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(422);
            _uowMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _uowMock.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_InactiveProduct_ReturnsFailure()
        {
            var customer = new CustomerBuilder().Build();
            var product = new ProductBuilder().AsInactive().Build();

            _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id)).ReturnsAsync(customer);
            _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

            var result = await _sut.CreateAsync(ValidCreateRequest(customer.Id, product.Id));

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task UpdateStatusAsync_ValidTransition_UpdatesStatus()
        {
            var order = new OrderBuilder().WithStatus(OrderStatus.Pending).Build();
            order.Customer = new CustomerBuilder().Build();
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);

            var result = await _sut.UpdateStatusAsync(order.Id, new UpdateOrderStatusRequest
            {
                NewStatus = OrderStatus.Confirmed,
                Notes = "Payment verified"
            });

            result.IsSuccess.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.Confirmed);
            _uowMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_InvalidTransition_ReturnsFailure()
        {
            var order = new OrderBuilder().WithStatus(OrderStatus.Delivered).Build();
            order.Customer = new CustomerBuilder().Build();
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);

            var result = await _sut.UpdateStatusAsync(order.Id, new UpdateOrderStatusRequest
            {
                NewStatus = OrderStatus.Cancelled
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            _uowMock.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusAsync_ConfirmOrder_DeductsStockReservations()
        {
            var product = new ProductBuilder().WithStock(100).WithReserved(10).Build();
            var item = new OrderItem
            {
                ProductId = product.Id,
                Quantity = 10,
                UnitPrice = 50M
            };
            var order = new OrderBuilder()
                .WithStatus(OrderStatus.Pending)
                .WithItems(new List<OrderItem> { item })
                .Build();
            order.Customer = new CustomerBuilder().Build();

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);
            _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

            await _sut.UpdateStatusAsync(order.Id, new UpdateOrderStatusRequest
            {
                NewStatus = OrderStatus.Confirmed
            });

            product.StockQuantity.Should().Be(90);
            product.ReservedQuantity.Should().Be(0);
        }

        [Fact]
        public async Task CancelAsync_PendingOrder_ReleasesReservationsAndCancels()
        {
            var product = new ProductBuilder().WithStock(100).WithReserved(5).Build();
            var item = new OrderItem
            {
                ProductId = product.Id,
                Quantity = 5,
                UnitPrice = 10m
            };
            var order = new OrderBuilder()
                .WithStatus(OrderStatus.Pending)
                .WithItems(new List<OrderItem> { item })
                .Build();

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);
            _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

            var result = await _sut.CancelAsync(order.Id, "Customer request");

            result.IsSuccess.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.Cancelled);
            product.ReservedQuantity.Should().Be(0);
            product.StockQuantity.Should().Be(100);
        }

        [Fact]
        public async Task CancelAsync_DeliveredOrder_ReturnsFailure()
        {
            var order = new OrderBuilder().WithStatus(OrderStatus.Delivered).Build();
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);

            var result = await _sut.CancelAsync(order.Id);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            _uowMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task CancelAsync_ShippedOrder_ReturnsFailure()
        {
            var order = new OrderBuilder().WithStatus(OrderStatus.Shipped).Build();
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);

            var result = await _sut.CancelAsync(order.Id);

            result.IsSuccess.Should().BeFalse();
            _uowMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }
    }
}
