using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.Services;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces.Repositories;
using ECommerce.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Services
{
    public class PaymentServiceTests : BaseTest
    {
        private readonly Mock<IStripeService> _stripeMock = new();
        private readonly Mock<IGenericRepository<Payment>> _paymentRepo = new();
        private readonly Mock<IGenericRepository<OrderStatusHistory>> _historyRepo = new();


        private readonly PaymentService _sut;

        private readonly StripeSettings _stripeSettings = new()
        {
            PublishableKey = "pk_test_fake",
            SecretKey = "sk_test_fake",
            WebhookSecret = "whsec_fake"
        };

        public PaymentServiceTests() : base()
        {
            _uowMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
            _uowMock.Setup(u => u.Repository<Payment>()).Returns(_paymentRepo.Object);
            _uowMock.Setup(u => u.Repository<OrderStatusHistory>())
                    .Returns(_historyRepo.Object);
            _uowMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _sut = new PaymentService(
                _uowMock.Object,
                _stripeMock.Object,
                _mapper,
                Options.Create(_stripeSettings),
                _loggerPaymentMock.Object);
        }

        private Order ConfirmedOrderWithId(Guid orderId)
        {
            var order = new OrderBuilder()
                .WithCustomerId(Guid.NewGuid())
                .WithStatus(OrderStatus.Confirmed)
                .Build();

            typeof(BaseEntity)
                .GetProperty("Id")!
                .SetValue(order, orderId);
            order.Customer = new CustomerBuilder().Build();
            return order;
        }

        [Fact]
        public async Task CreatePaymentIntent_ConfirmedOrder_CreatesIntentAndReturnsClientSecret()
        {
            var orderId = Guid.NewGuid();
            var order = ConfirmedOrderWithId(orderId);

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(orderId)).ReturnsAsync(order);
            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(new List<Payment>());
            _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);

            _stripeMock
                .Setup(s => s.CreatePaymentIntentAsync(orderId, order.Total, "usd"))
                .ReturnsAsync(new StripePaymentIntentResult
                {
                    PaymentIntentId = "pi_test_abc",
                    ClientSecret = "pi_test_abc_secret_xyz",
                    Status = "requires_payment_method",
                    Amount = order.Total
                });

            var result = await _sut.CreatePaymentIntentAsync(
                new CreatePaymentIntentRequest { OrderId = orderId });

            result.IsSuccess.Should().BeTrue();
            result.Value!.ClientSecret.Should().Be("pi_test_abc_secret_xyz");
            result.Value.PublishableKey.Should().Be("pk_test_fake");
            result.Value.PaymentIntentId.Should().Be("pi_test_abc");

            _paymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreatePaymentIntent_PendingOrder_ReturnsFailure()
        {
            var orderId = Guid.NewGuid();
            var order = new OrderBuilder()
                .WithStatus(OrderStatus.Pending)
                .Build();

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(orderId)).ReturnsAsync(order);

            var result = await _sut.CreatePaymentIntentAsync(
                new CreatePaymentIntentRequest { OrderId = orderId });

            result.IsSuccess!.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            _stripeMock.Verify(
                s => s.CreatePaymentIntentAsync(
                    It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CreatePaymentIntent_AlreadyPaidOrder_ReturnsFailure()
        {
            var orderId = Guid.NewGuid();
            var order = ConfirmedOrderWithId(orderId);
            var payment = new PaymentBuilder()
                .WithOrderId(orderId)
                .AsSucceeded() 
                .Build();

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(orderId)).ReturnsAsync(order);
            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
                         .ReturnsAsync(new List<Payment> { payment });

            var result = await _sut.CreatePaymentIntentAsync(
                new CreatePaymentIntentRequest { OrderId = orderId });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Error.Should().Contain("This order has already been paid.");
        }

        [Fact]
        public async Task CreatePaymentIntent_OrderNotFound_Returns404()
        {
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((Order?)null);

            var result = await _sut.CreatePaymentIntentAsync(
                new CreatePaymentIntentRequest { OrderId = Guid.NewGuid() });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreatePaymentIntent_ExistingPendingPayment_ReusesIntent()
        {
            var orderId = Guid.NewGuid();
            var order = ConfirmedOrderWithId(orderId);
            var existingIntent = "pi_existing_123";
            var payment = new PaymentBuilder()
                .WithOrderId(orderId)
                .WithIntentId(existingIntent)
                .WithStatus(PaymentStatus.Pending)
                .Build();

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(orderId)).ReturnsAsync(order);
            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
                         .ReturnsAsync(new List<Payment> { payment });

            _stripeMock
                .Setup(s => s.GetPaymentIntentAsync(existingIntent))
                .ReturnsAsync(new StripePaymentIntentResult
                {
                    PaymentIntentId = existingIntent,
                    ClientSecret = "pi_existing_secret",
                    Status = "requires_payment_method",
                    Amount = order.Total
                });

            var result = await _sut.CreatePaymentIntentAsync(
                new CreatePaymentIntentRequest { OrderId = orderId });

            result.IsSuccess.Should().BeTrue();
            result.Value!.PaymentIntentId.Should().Be(existingIntent);

            _stripeMock.Verify(
                s => s.CreatePaymentIntentAsync(
                    It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleWebhook_PaymentSucceeded_UpdatesPaymentAndAdvancesOrder()
        {
            var orderId = Guid.NewGuid();
            var intentId = "pi_test_succeeded";
            var payment = new PaymentBuilder()
                .WithOrderId(orderId)
                .WithIntentId(intentId)
                .WithStatus(PaymentStatus.Pending)
                .Build();
            var order = new OrderBuilder()
                .WithStatus(OrderStatus.Confirmed)
                .WithItems(new List<OrderItem>())
                .Build();
            order.Customer = new CustomerBuilder().Build();

            _stripeMock
                .Setup(s => s.ParseWebhookEvent("payload", "sig"))
                .Returns(new StripeWebhookResult
                {
                    EventType = StripeWebhookResult.PaymentSucceeded,
                    PaymentIntentId = intentId,
                    Status = "succeeded",
                    Amount = 118m
                });

            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(new List<Payment> { payment });

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(orderId)).ReturnsAsync(order);

            var result = await _sut.HandleWebhookAsync("payload", "sig");

            result.IsSuccess.Should().BeTrue();
            payment.Status.Should().Be(PaymentStatus.Succeeded);
            payment.PaidAt.Should().NotBeNull();
            order.Status.Should().Be(OrderStatus.Processing);
            _uowMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task HandleWebhook_PaymentFailed_UpdatesPaymentStatusOnly()
        {
            var intentId = "pi_test_failed";
            var orderId = Guid.NewGuid();
            var payment = new PaymentBuilder()
                .WithOrderId(orderId)
                .WithIntentId(intentId)
                .WithStatus(PaymentStatus.Pending)
                .Build();

            _stripeMock
                .Setup(s => s.ParseWebhookEvent("payload", "sig"))
                .Returns(new StripeWebhookResult
                {
                    EventType = StripeWebhookResult.PaymentFailed,
                    PaymentIntentId = intentId,
                });

            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(new List<Payment> { payment });

            var result = await _sut.HandleWebhookAsync("payload", "sig");

            result.IsSuccess.Should().BeTrue();
            payment.Status.Should().Be(PaymentStatus.Failed);
        }

        [Fact]
        public async Task HandleWebhook_InvalidSignature_ReturnsFailure()
        {
            _stripeMock
                .Setup(s => s.ParseWebhookEvent(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((StripeWebhookResult?)null);

            var result = await _sut.HandleWebhookAsync("payload", "bad_sig");

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            _uowMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task HandleWebhook_UnknownPaymentIntent_ReturnsTrueWithoutError()
        {
            _stripeMock
                .Setup(s => s.ParseWebhookEvent("payload", "sig"))
                .Returns(new StripeWebhookResult
                {
                    EventType = StripeWebhookResult.PaymentSucceeded,
                    PaymentIntentId = "pi_unknown_999"
                });

            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(new List<Payment>());

            var result = await _sut.HandleWebhookAsync("payload", "sig");

            result.IsSuccess.Should().BeTrue();
            _uowMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task RefundAsync_SucceededPayment_IssuesRefundAndCancelsOrder()
        {
            var orderId = Guid.NewGuid();
            var intentId = "pi_test_refund";
            var payment = new PaymentBuilder()
                .WithOrderId(orderId)
                .WithIntentId(intentId)
                .AsSucceeded()
                .Build();
            var order = new OrderBuilder()
                .WithStatus(OrderStatus.Confirmed)
                .WithItems(new List<OrderItem>())
                .Build();
            order.Customer = new CustomerBuilder().Build();

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(orderId)).ReturnsAsync(order);

            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(new List<Payment> { payment });

            _stripeMock.Setup(s => s.RefundPaymentAsync(intentId)).ReturnsAsync(true);

            var result = await _sut.RefundAsync(orderId);

            result.IsSuccess.Should().BeTrue();
            payment.Status.Should().Be(PaymentStatus.Refunded);
            order.Status.Should().Be(OrderStatus.Cancelled);
            _uowMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task RefundAsync_NotYetPaid_ReturnsFailure()
        {
            var orderId = Guid.NewGuid();
            var payment = new PaymentBuilder()
                .WithOrderId(orderId)
                .WithStatus(PaymentStatus.Pending)
                .Build();
            var order = new OrderBuilder().WithStatus(OrderStatus.Confirmed).Build();
            order.Customer = new CustomerBuilder().Build();

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(orderId)).ReturnsAsync(order);
            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(new List<Payment> { payment });

            var result = await _sut.RefundAsync(orderId);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            _stripeMock.Verify(
                s => s.RefundPaymentAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RefundAsync_StripeRefundFails_ReturnsFailure()
        {
            var orderId = Guid.NewGuid();
            var intentId = "pi_test_fail_refund";
            var payment = new PaymentBuilder()
                .WithOrderId(orderId)
                .WithIntentId(intentId)
                .AsSucceeded()
                .Build();
            var order = new OrderBuilder()
                .WithStatus(OrderStatus.Confirmed).Build();
            order.Customer = new CustomerBuilder().Build();

            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(orderId)).ReturnsAsync(order);
            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(new List<Payment> { payment });
            _stripeMock.Setup(s => s.RefundPaymentAsync(intentId)).ReturnsAsync(false);

            var result = await _sut.RefundAsync(orderId);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            _uowMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task GetPaymentByOrder_ExistingPayment_ReturnsPaymentResponse()
        {
            var orderId = Guid.NewGuid();
            var payment = new PaymentBuilder()
                .WithOrderId(orderId)
                .AsSucceeded()
                .Build();

            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(new List<Payment> { payment });

            var result = await _sut.GetPaymentByOrderAsync(orderId);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Status.Should().Be(PaymentStatus.Succeeded);
            result.Value.OrderId.Should().Be(orderId);
        }

        [Fact]
        public async Task GetPaymentByOrder_NoPayment_Returns404()
        {
            _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
                        .ReturnsAsync(new List<Payment>());

            var result = await _sut.GetPaymentByOrderAsync(Guid.NewGuid());
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }
    }
}
