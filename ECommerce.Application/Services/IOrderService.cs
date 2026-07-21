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
    public interface IOrderService
    {
        Task<Result<PagedResponse<OrderResponse>>> GetAllAsync(OrderQueryParams query);
        Task<Result<OrderResponse>> GetByIdAsync(Guid id);
        Task<Result<OrderResponse>> CreateAsync(CreateOrderRequest request);
        Task<Result<OrderResponse>> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request);
        Task<Result<bool>> CancelAsync(Guid id, string? reason = null);
    }
}
