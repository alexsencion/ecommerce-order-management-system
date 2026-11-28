using AutoMapper;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Response;
using ECommerce.Application.Models;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
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
    public class InvoiceService : IInvoiceService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new() { PropertyNameCaseInsensitive = true };

        private readonly IUnitOfWork _uow;
        private readonly IInvoicePdfGenerator _pdf;
        private readonly IFileStorage _storage;
        private readonly IMapper _mapper;
        private readonly TimeProvider _clock;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(
            IUnitOfWork uow, 
            IInvoicePdfGenerator pdf, 
            IFileStorage storage, 
            IMapper mapper, 
            TimeProvider clock, 
            ILogger<InvoiceService> logger)
        {
            _uow = uow;
            _pdf = pdf;
            _storage = storage;
            _mapper = mapper;
            _clock = clock;
            _logger = logger;
        }

        public async Task<Result<InvoiceResponse>> GenerateAsync(Guid orderId)
        {
            var order = await _uow.Orders.GetWithDetailsAsync(orderId);
            if (order == null)
                return Result<InvoiceResponse>.NotFound($"Order {orderId} not found.");

            var existing = await _uow.Invoices.GetByOrderIdAsync(orderId);
            if (existing != null)
                return Result<InvoiceResponse>.Success(_mapper.Map<InvoiceResponse>(existing));

            if (!OrderStatusGroups.IsPaid(order.Status))
                return Result<InvoiceResponse>.Failure(
                    $"Invoices can only be issued for paid orders. " +
                    $"Current status: {order.Status}.");

            var now = _clock.GetUtcNow().UtcDateTime;
            var sequence = await _uow.Invoices.CountIssuedInMonthAsync(now.Year, now.Month) + 1;
            var invoice = Invoice.Create(order, sequence, now);

            var pdf = _pdf.Generate(BuildDocument(order, invoice));
            await _storage.SaveAsync(invoice.StoragePath, pdf);

            try
            {
                await _uow.Invoices.AddAsync(invoice);
                await _uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _storage.DeleteAsync(invoice.StoragePath);
                _logger.LogError(ex, "Failed to persist invoice for order {OrderId}", orderId);
                throw;
            }

            _logger.LogInformation(
                "Invoice {InvoiceNumber} issued for order {OrderId}",
                invoice.InvoiceNumber, orderId);

            return Result<InvoiceResponse>.Created(_mapper.Map<InvoiceResponse>(invoice));
        }

        public async Task<Result<InvoiceResponse>> GetByOrderAsync(Guid orderId)
        {
            var invoice = await _uow.Invoices.GetByOrderIdAsync(orderId);
            return invoice == null
                ? Result<InvoiceResponse>.NotFound($"No invoice found for order {orderId}.")
                : Result<InvoiceResponse>.Success(_mapper.Map<InvoiceResponse>(invoice));
        }

        public async Task<Result<InvoiceFile>> DownloadAsync(Guid invoiceId)
        {
            var invoice = await _uow.Invoices.GetByIdAsync(invoiceId);
            if (invoice == null)
                return Result<InvoiceFile>.NotFound($"Invoice {invoiceId} not found.");

            var content = await _storage.ReadAsync(invoice.StoragePath);
            if (content == null)
            {
                _logger.LogError(
                    "Invoice {InvoiceNumber} exists but its file is missing at {Path}",
                    invoice.InvoiceNumber, invoice.StoragePath);
                return Result<InvoiceFile>.NotFound("Invoice document is missing from storage.");
            }

            return Result<InvoiceFile>.Success(new InvoiceFile(content, invoice.FileName));
        }


        private static InvoiceDocument BuildDocument(Order order, Invoice invoice)
        {
            var shipTo = JsonSerializer.Deserialize<ShippingAddressDto>(
                             order.ShippingAddressJson, JsonOptions)
                         ?? new ShippingAddressDto();
            
            var lines = order.Items
                .Select(i => new InvoiceLine(
                    i.Product?.Name ?? "Product",
                    i.Product?.Sku ?? "-",
                    i.Quantity,
                    i.UnitPrice,
                    i.Quantity * i.UnitPrice))
                .ToList();

            return new InvoiceDocument(
                invoice.InvoiceNumber,
                invoice.IssuedAt,
                order.Id.ToString("N")[..8].ToUpperInvariant(),
                invoice.BillToName,
                invoice.BillToEmail,
                shipTo,
                lines,
                invoice.Subtotal,
                invoice.Tax,
                invoice.Total
                );
        }

    }
}
