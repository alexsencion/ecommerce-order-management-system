using ECommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class Invoice : BaseEntity
    {
        public Guid OrderId { get; private set; }
        public string InvoiceNumber { get; private set; } = string.Empty;
        public DateTime IssuedAt { get; private set; }
        public string BillToName { get; private set; } = string.Empty;
        public string BillToEmail { get; private set; } = string.Empty;
        public decimal Subtotal { get; private set; }
        public decimal Tax { get; private set; }
        public decimal Total { get; private set; }
        public string StoragePath { get; private set; } = string.Empty;
        public string FileName => $"{InvoiceNumber}.pdf";
        public Order Order { get; private set; } = null!;

        private Invoice() { }

        public static string FormatNumber(DateTime issuedAt, int sequence)
        {
            if (sequence < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(sequence), "Invoice sequence must start at 1.");

            return $"INV-{issuedAt:yyyyMM}-{sequence:D5}";
        }

        public static Invoice Create(Order order, int sequence, DateTime issuedAt)
        {
            ArgumentNullException.ThrowIfNull(order);

            if (!OrderStatusGroups.IsPaid(order.Status))
                throw new InvalidOperationException(
                    $"Invoices can only be issued for paid orders. " +
                    $"Current status: {order.Status}.");

            if (order.Customer is null)
                throw new InvalidOperationException(
                    "Order customer must be loaded to issue an invoice.");

            var number = FormatNumber(issuedAt, sequence);

            return new Invoice
            {
                OrderId = order.Id,
                InvoiceNumber = number,
                IssuedAt = issuedAt,
                BillToName = order.Customer.FullName,
                BillToEmail = order.Customer.Email,
                Subtotal = order.Subtotal,
                Tax = order.Tax,
                Total = order.Total,
                StoragePath = $"invoices/{issuedAt:yyyy}/{issuedAt:MM}/{number}.pdf"
            };
        }
    }
}
