using AutoMapper;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IStripeService _stripe;
        private readonly IMapper _mapper;
        private readonly StripeSettings _stripeSettings;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IUnitOfWork uow, IStripeService stripe, IMapper mapper, IOptions<StripeSettings> options, ILogger<PaymentService> logger)
        {
            _uow = uow;
            _stripe = stripe;
            _mapper = mapper;
            _stripeSettings = options.Value;
            _logger = logger;
        }

        public async Task<Result<PaymentIntentResponse>> CreatePaymentIntentAsync(CreatePaymentIntentRequest request)
        {
            _logger.LogInformation("Stripe Secret Key: {Key}", _stripeSettings.SecretKey);

            var order = await _uow.Orders.GetWithDetailsAsync(request.OrderId);
            if (order == null)
                return Result<PaymentIntentResponse>.NotFound(
                    $"Order {request.OrderId} not found.");

            if (order.Status != OrderStatus.Confirmed)
                return Result<PaymentIntentResponse>.Failure(
                    $"Only confirmed orders can be paid. " +
                    $"Current status: {order.Status}.");

            

            var existing = await _uow.Repository<Payment>()
                .FindAsync(p => p.OrderId == request.OrderId);

            var existingPayment = existing.FirstOrDefault();
            if (existingPayment?.Status == PaymentStatus.Succeeded)
                return Result<PaymentIntentResponse>.Failure(
                    "This order has already been paid.");

            try
            {
                StripePaymentIntentResult intent;

                if (existingPayment != null && !string.IsNullOrEmpty(existingPayment.StripePaymentIntentId))
                {
                    intent = await _stripe.GetPaymentIntentAsync(
                        existingPayment.StripePaymentIntentId);
                }
                else
                {
                    intent = await _stripe.CreatePaymentIntentAsync(order.Id, order.Total);

                    var payment = new Payment
                    {
                        OrderId = order.Id,
                        StripePaymentIntentId = intent.PaymentIntentId,
                        Status = PaymentStatus.Pending,
                        Amount = order.Total
                    };
                    await _uow.Repository<Payment>().AddAsync(payment);
                    await _uow.SaveChangesAsync();
                }

                return Result<PaymentIntentResponse>.Success(new PaymentIntentResponse()
                {
                    PaymentIntentId = intent.PaymentIntentId,
                    ClientSecret = intent.ClientSecret,
                    PublishableKey = _stripeSettings.PublishableKey,
                    Amount = intent.Amount,
                    Currency = "usd",
                    Status = intent.Status,
                });
            }
            catch (Exception ex)
            {
                return Result<PaymentIntentResponse>.Failure(ex.Message, 502);
            }
        }

        public async Task<Result<PaymentResponse>> GetPaymentByOrderAsync(Guid orderId)
        {
            var payments = await _uow.Repository<Payment>()
                .FindAsync(p => p.OrderId == orderId);

            var payment = payments.FirstOrDefault();
            if (payment == null)
                return Result<PaymentResponse>.NotFound(
                    $"No payment found for order {orderId}.");

            return Result<PaymentResponse>.Success(_mapper.Map<PaymentResponse>(payment));
        }

        public async Task<Result<bool>> HandleWebhookAsync(string payload, string stripeSignature)
        {
            var webhookEvent = _stripe.ParseWebhookEvent(payload, stripeSignature);

            if (webhookEvent == null)
                return Result<bool>.Failure("Invalid webhook signature.", 400);

            if (string.IsNullOrEmpty(webhookEvent.PaymentIntentId))
            {
                _logger.LogInformation(
                    "Ignoring Stripe event {EventType} because it has no PaymentIntentId",
                    webhookEvent.EventType);
                return Result<bool>.Success(true);
            }

            var payments = await _uow.Repository<Payment>()
                .FindAsync(p => p.StripePaymentIntentId == webhookEvent.PaymentIntentId);

            var payment = payments.FirstOrDefault();
            if (payment == null)
            {
                _logger.LogWarning(
                    "Webhook received for unknown PaymentIntent: {PaymentIntentId}", 
                    webhookEvent.PaymentIntentId);
                return Result<bool>.Success(true);
            }

            await _uow.BeginTransactionAsync();
            try
            {
                switch (webhookEvent.EventType)
                {
                    case StripeWebhookResult.PaymentSucceeded:
                        _logger.LogInformation("Processing payment success for Payment {PaymentId}",
                            payment.Id);
                        await HandlePaymentSucceded(payment);

                        _logger.LogInformation("Payment success processed for Payment {PaymentId}",
                            payment.Id);
                        break;

                    case StripeWebhookResult.PaymentFailed:
                        await HandlePaymentFailed(payment);
                        break;

                    case StripeWebhookResult.PaymentCancelled:
                        await HandlePaymentCancelled(payment);
                        break;

                    default:
                        _logger.LogInformation(
                            "Unhandled Stripe event type: {EventType}",
                            webhookEvent.EventType);
                        return Result<bool>.Success(true);
                }

                await _uow.CommitTransactionAsync();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                _logger.LogError(ex,
                    "Error handling Stripe webhook for PaymentIntent {PaymentIntentId}", webhookEvent.PaymentIntentId);
                throw;
            }
        }

        public async Task<Result<bool>> RefundAsync(Guid orderId)
        {
            var order = await _uow.Orders.GetWithDetailsAsync(orderId);
            if (order == null)
                return Result<bool>.NotFound($"Order {orderId} not found.");

            var payments = await _uow.Repository<Payment>()
                .FindAsync(p =>  p.OrderId == orderId);

            var payment = payments.FirstOrDefault();
            if (payment == null)
                return Result<bool>.NotFound("No payment found for this order.");

            if (payment.Status != PaymentStatus.Succeeded)
                return Result<bool>.Failure(
                    "Only succeded payments can be refunded.");

            var refunded = await _stripe.RefundPaymentAsync(
                payment.StripePaymentIntentId);

            if (!refunded)
                return Result<bool>.Failure(
                    "Refund failed. Please retry or process manually in Stripe.");

            await _uow.BeginTransactionAsync();
            try
            {
                payment.Status = PaymentStatus.Refunded;
                _uow.Repository<Payment>().Update(payment);

                var history = order.Transition(
                    OrderStatus.Cancelled, "Refunded via Stripe.");
                await _uow.Repository<OrderStatusHistory>().AddAsync(history);
                _uow.Orders.Update(order);

                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();

                _logger.LogInformation(
                    "Payment refunded and order cancelled: {OrderId}", orderId);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                _logger.LogError(ex, "Error completing refund for order {OrderId}", orderId);
                throw;
            }
        }

        private async Task HandlePaymentSucceded(Payment payment)
        {
            _logger.LogInformation(
                "Starting payment success handler for payment {PaymentId}",
                payment.Id);

            payment.Status = PaymentStatus.Succeeded;
            payment.PaidAt = DateTime.UtcNow;

            _uow.Repository<Payment>().Update(payment);

            _logger.LogInformation(
                "Payment updated, loading order {OrderId}",
                payment.OrderId);

            var order = await _uow.Orders.GetWithDetailsAsync(payment.OrderId);

            if (order == null)
            {
                _logger.LogWarning(
                    "Order not found {OrderId}",
                    payment.OrderId);
            }
            else
            {
                _logger.LogInformation(
                "Order {OrderId} status is {Status}",
                order.Id,
                order.Status);
            }

            if (order != null && order.Status == OrderStatus.Confirmed)
            {
                var history = order.Transition(
                    OrderStatus.Processing, "Payment confirmed via Stripe.");
                order.StatusHistory ??= new List<OrderStatusHistory>();
                await _uow.Repository<OrderStatusHistory>().AddAsync(history);
                _uow.Orders.Update(order);
            }

            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "Payment succeeded for order {OrderId}", payment.OrderId);
        }

        private async Task HandlePaymentFailed(Payment payment)
        {
            payment.Status = PaymentStatus.Failed;
            _uow.Repository<Payment>().Update(payment);
            await _uow.SaveChangesAsync();

            _logger.LogWarning(
                "Payment failed for order {OrderId}. PaymentIntent: {PaymentIntentId}", payment.OrderId, payment.StripePaymentIntentId);
        }

        private async Task HandlePaymentCancelled(Payment payment)
        {
            payment.Status = PaymentStatus.Failed;
            _uow.Repository<Payment>().Update(payment);

            var order = await _uow.Orders.GetWithDetailsAsync(payment.OrderId);
            if (order != null && order.Status is OrderStatus.Confirmed or OrderStatus.Pending)
            {
                var history = order.Transition(
                    OrderStatus.Cancelled, "Payment cancelled via Stripe.");
                order.StatusHistory.Add(history);
                _uow.Orders.Update(order);
            }

            await _uow.SaveChangesAsync();
        }
    }
}
