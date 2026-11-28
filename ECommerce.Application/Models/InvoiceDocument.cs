using ECommerce.Application.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Models
{
    public record InvoiceLine(
        string Description,
        string Sku,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);

    public record InvoiceDocument(
        string InvoiceNumber,
        DateTime IssuedAt,
        string OrderReference,
        string BillToName,
        string BillToEmail,
        ShippingAddressDto ShipTo,
        IReadOnlyList<InvoiceLine> Lines,
        decimal Subtotal,
        decimal Tax,
        decimal Total);
}
