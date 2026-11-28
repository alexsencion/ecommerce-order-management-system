using ECommerce.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public interface IInvoicePdfGenerator
    {
        byte[] Generate(InvoiceDocument document);
    }
}
