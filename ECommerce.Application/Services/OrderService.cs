using AutoMapper;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;

        private const decimal TaxRate = 0.18m;

        public OrderService(IUnitOfWork uow, IMapper mapper, ILogger<OrderService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<bool>> CancelAsync(Guid id, string? reason = null)
        {
            var order = await _uow.Orders.GetWithDetailsAsync(id);
            if (order == null)
                return Result<bool>.NotFound($"Order {id} not found.");

            if (OrderStateMachine.IsTerminal(order.Status))
                return Result<bool>.Failure(
                    $"Order is already in a terminal state ({order.Status}) and cannot be cancelled.");

            if (!OrderStateMachine.CanTransition(order.Status, OrderStatus.Cancelled))
                return Result<bool>.Failure(
                    $"Order cannot be cancelled from its current state ({order.Status}).");

            await _uow.BeginTransactionAsync();
            try
            {
                if (order.Status == OrderStatus.Pending)
                {
                    foreach (var item in order.Items)
                    {
                        var product = await _uow.Products.GetByIdAsync(item.ProductId);
                        if (product != null)
                        {
                            StockReservation.Release(product, item.Quantity);
                            _uow.Products.Update(product);
                        }
                    }
                }

                var history = order.Transition(OrderStatus.Cancelled,
                    reason ?? "Cancelled by request.");
                order.StatusHistory.Add(history);
                _uow.Orders.Update(order);
                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();

                _logger.LogInformation("Order {OrderId} cancelled. Reason: {Reason}",
                    id, reason ?? "none");

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                throw;
            }
        }

        public async Task<Result<OrderResponse>> CreateAsync(CreateOrderRequest request)
        {
            var customer = await _uow.Customers.GetByIdAsync(request.CustomerId);
            if (customer == null)
                return Result<OrderResponse>.NotFound(
                    $"Customer {request.CustomerId} not found");

            if (!customer.IsActive)
                return Result<OrderResponse>.Failure(
                    "Inactive customers cannot place orders.");

            var productIds = request.Items.Select(i => i.ProductId).ToList();
            var products = new Dictionary<Guid, Product>();

            foreach (var productId in productIds)
            {
                var product = await _uow.Products.GetByIdAsync(productId);
                if (product == null)
                    return Result<OrderResponse>.NotFound(
                        $"Product {productId} not found.");

                if (!product.IsActive)
                    return Result<OrderResponse>.Failure(
                        $"Product {product.Name} is not available for purchase.");

                products[productId] = product;
            }

            await _uow.BeginTransactionAsync();
            try
            {
                var orderItems = new List<OrderItem>();

                foreach (var itemReq in request.Items)
                {
                    var product = products[itemReq.ProductId];

                    StockReservation.Reserve(product, itemReq.Quantity);
                    _uow.Products.Update(product);

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = itemReq.Quantity,
                        UnitPrice = product.Price
                    });
                }

                var subtotal = orderItems.Sum(i => i.LineTotal);
                var tax = Math.Round(subtotal * TaxRate, 2);
                var total = subtotal + tax;

                var order = new Order
                {
                    CustomerId = customer.Id,
                    Status = OrderStatus.Pending,
                    Items = orderItems,
                    Subtotal = subtotal,
                    Tax = tax,
                    Total = total,
                    ShippingAddressJson = JsonSerializer.Serialize(request.ShippingAddress),
                    Notes = request.Notes,
                };

                await _uow.Orders.AddAsync(order);
                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();

                _logger.LogInformation(
                    "Order created: {OrderId} for customer {CustomerId}. Total: {Total}",
                    order.Id, customer.Id, total);

                var created = await _uow.Orders.GetWithDetailsAsync(order.Id);
                return Result<OrderResponse>.Created(_mapper.Map<OrderResponse>(created!));
            }
            catch (InvalidOperationException ex)
            {
                await _uow.RollbackTransactionAsync();
                return Result<OrderResponse>.Failure(ex.Message, 422);
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                _logger.LogError(ex, "Unexpected error creating order for customer {CustomerId}",
                    request.CustomerId);
                throw;
            }
        }

        public async Task<Result<PagedResponse<OrderResponse>>> GetAllAsync(OrderQueryParams query)
        {
            var orders = await _uow.Orders.GetAllAsync();

            if (query.CustomerId.HasValue)
                orders = orders.Where(o => o.CustomerId == query.CustomerId.Value);

            if (query.Status.HasValue)
                orders = orders.Where(o => o.Status == query.Status.Value);

            if (query.From.HasValue)
                orders = orders.Where(o => o.CreatedAt == query.From.Value);

            if (query.To.HasValue)
                orders = orders.Where(o => o.CreatedAt == query.To.Value);

            var totalCount = orders.Count();
            var pagedSize = query.ClampedPageSize;

            var paged = orders
                .OrderByDescending(o => o.CreatedAt)
                .Skip((query.Page - 1) * pagedSize)
                .Take(pagedSize)
                .ToList();

            return Result<PagedResponse<OrderResponse>>.Success(new PagedResponse<OrderResponse>
            {
                Data = _mapper.Map<IEnumerable<OrderResponse>>(paged),
                Page = query.Page,
                PageSize = pagedSize,
                TotalCount = totalCount
            });
        }

        public async Task<Result<OrderResponse>> GetByIdAsync(Guid id)
        {
            var order = await _uow.Orders.GetWithDetailsAsync(id);
            if (order == null)
                return Result<OrderResponse>.NotFound($"Order {id} not found.");

            return Result<OrderResponse>.Success(_mapper.Map<OrderResponse>(order));
        }

        public async Task<Result<OrderResponse>> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request)
        {
            var order = await _uow.Orders.GetWithDetailsAsync(id);
            if (order == null)
                return Result<OrderResponse>.NotFound($"Order {id} not found.");

            if (!OrderStateMachine.CanTransition(order.Status, request.NewStatus))
            {
                var allowed = OrderStateMachine.GetAllowedTransitions(order.Status);
                return Result<OrderResponse>.Failure(
                    $"Cannot transation from {order.Status} to {request.NewStatus}. " +
                    $"Allowed: {string.Join(", ", allowed)}.");
            }

            await _uow.BeginTransactionAsync();
            try
            {
                if (request.NewStatus == OrderStatus.Confirmed)
                {
                    foreach (var item in order.Items)
                    {
                        var product = await _uow.Products.GetByIdAsync(item.ProductId);
                        if (product != null)
                        {
                            StockReservation.Confirm(product, item.Quantity);
                            _uow.Products.Update(product);
                        }
                    }
                }

                var history = order.Transition(request.NewStatus, request.Notes);
                order.StatusHistory.Add(history);
                _uow.Orders.Update(order);
                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();

                _logger.LogInformation(
                    "Order {OrderId} transitioned: {From} -> {To}",
                    id, history.FromStatus, history.ToStatus);

                return Result<OrderResponse>.Success(_mapper.Map<OrderResponse>(order));
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating status for order {OrderId}", id);
                throw;
            }
        }
    }
}
