using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public interface IPaymentService
    {
        Task<Result<PaymentIntentResponse>> CreatePaymentIntentAsync(CreatePaymentIntentRequest request);

        Task<Result<PaymentResponse>> GetPaymentByOrderAsync(Guid orderId);

        Task<Result<bool>> HandleWebhookAsync(string payload, string stripeSignature);

        Task<Result<bool>> RefundAsync(Guid orderId);
    }
}
