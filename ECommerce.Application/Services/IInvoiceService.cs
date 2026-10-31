using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Response;
using ECommerce.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public interface IInvoiceService
    {
        Task<Result<InvoiceResponse>> GenerateAsync(Guid orderId);
        Task<Result<InvoiceResponse>> GetByOrderAsync(Guid orderId);
        Task<Result<InvoiceFile>> DownloadAsync(Guid invoiceId);
    }
}
