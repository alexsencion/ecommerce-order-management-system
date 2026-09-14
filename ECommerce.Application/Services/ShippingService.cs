using AutoMapper;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public class ShippingService : IShippingService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<ShippingService> _logger;

        public ShippingService(
            IUnitOfWork uow,
            IMapper mapper,
            ILogger<ShippingService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<ShipmentResponse>> CreateShipmentAsync(CreateShipmentRequest request)
        {
            var order = await _uow.Orders.GetWithDetailsAsync(request.OrderId);
            if (order == null)
                return Result<ShipmentResponse>.NotFound(
                    $"Order {request.OrderId} not found.");

            if (order.Status != OrderStatus.Packed)
                return Result<ShipmentResponse>.Failure(
                    $"Only packed orders can be shipped. " +
                    $"Current status: {order.Status}.");

            if (order.Shipment != null)
                return Result<ShipmentResponse>.Conflict(
                    "A shipment already exists for this order.");

            await _uow.BeginTransactionAsync();
            try
            {
                var shipment = Shipment.Create(
                    order.Id,
                    request.Carrier,
                    request.TrackingNumber,
                    request.Notes);

                await _uow.Repository<Shipment>().AddAsync(shipment);

                var histoy = order.Transition(
                    OrderStatus.Shipped,
                    $"Shipped via {request.Carrier}. " +
                    $"Tracking: {request.TrackingNumber}");
                order.StatusHistory.Add(histoy);
                _uow.Orders.Update(order);

                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();

                _logger.LogInformation(
                    "Shipment creaed for order {OrderId}. " +
                    "Carrier: {Carrier}, Tracking: {TrackingNumber}",
                    order.Id, request.Carrier, request.TrackingNumber);

                return Result<ShipmentResponse>.Created(
                    _mapper.Map<ShipmentResponse>(shipment));
            }
            catch (ArgumentException ex)
            {
                await _uow.RollbackTransactionAsync();
                return Result<ShipmentResponse>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                _logger.LogError(ex,
                    "Error creating shipment for order {OrderId}", request.OrderId);
                throw;
            }
        }

        public async Task<Result<ShipmentResponse>> GetByOrderAsync(Guid orderId)
        {
            var order = await _uow.Orders.GetWithDetailsAsync(orderId);
            if (order == null)
                return Result<ShipmentResponse>.NotFound(
                    $"Order {orderId} not found.");

            if (order.Shipment == null)
                return Result<ShipmentResponse>.NotFound(
                    $"No shipment found for order {orderId}.");

            return Result<ShipmentResponse>.Success(
                    _mapper.Map<ShipmentResponse>(order.Shipment));
        }

        public async Task<Result<ShipmentResponse>> UpdateShipmentAsync(
            Guid shipmentId, UpdateShipmentRequest request)
        {
            var shipment = await _uow.Repository<Shipment>().GetByIdAsync(shipmentId);
            if (shipment == null)
                return Result<ShipmentResponse>.NotFound(
                    $"Shipment {shipmentId} not found.");

            if (shipment.Status == ShipmentStatus.Delivered)
                return Result<ShipmentResponse>.Failure(
                    "Cannot update a shipment that has already been delivered.");

            shipment.Carrier = request.Carrier.Trim();
            shipment.TrackingNumber = request.TrackingNumber.Trim();
            shipment.Notes = request.Notes?.Trim();

            _uow.Repository<Shipment>().Update(shipment);
            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "Shipment {ShipmentId} updated. " +
                "Carrier: {Carrier}, Tracking: {TrackingNumber}",
                shipmentId, request.Carrier, request.TrackingNumber);

            return Result<ShipmentResponse>.Success(
                _mapper.Map<ShipmentResponse>(shipment));
        }

        public async Task<Result<ShipmentResponse>> MarkDeliveredAsync(Guid shipmentId)
        {
            var shipment = await _uow.Repository<Shipment>().GetByIdAsync(shipmentId);
            if (shipment == null)
                return Result<ShipmentResponse>.NotFound(
                    $"Shipment {shipmentId} not found.");

            await _uow.BeginTransactionAsync();
            try
            {
                shipment.MarkDelivered();
                _uow.Repository<Shipment>().Update(shipment);

                var order = await _uow.Orders.GetWithDetailsAsync(shipment.OrderId);
                if (order != null && order.Status == OrderStatus.Shipped)
                {
                    var history = order.Transition(
                        OrderStatus.Delivered, "Delivery confirmed.");
                    order.StatusHistory.Add(history);
                    _uow.Orders.Update(order);
                }

                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();

                _logger.LogInformation(
                    "Shipment {ShipmentId} marked as delivered for order {OrderId}", 
                    shipmentId, shipment.OrderId);

                return Result<ShipmentResponse>.Success(
                    _mapper.Map<ShipmentResponse>(shipment));
            }
            catch (InvalidOperationException ex)
            {
                await _uow.RollbackTransactionAsync();
                return Result<ShipmentResponse>.Failure(ex.Message);
            }
            catch (Exception ex) 
            {
                await _uow.RollbackTransactionAsync();
                _logger.LogError(ex,
                    "Error marking shipment {ShipmentId} as delivered", shipmentId);
                throw;
            }
        }
    }
}
