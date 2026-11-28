using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.DTOs.Response
{
    public class InvoiceResponse
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string BillToName { get; set; } = string.Empty;
        public string BillToEmail { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string FileName { get; set; } = string.Empty;
    }
}
