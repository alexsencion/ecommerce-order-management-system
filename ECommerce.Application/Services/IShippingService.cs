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
    public interface IShippingService
    {
        Task<Result<ShipmentResponse>> CreateShipmentAsync(CreateShipmentRequest request);
        Task<Result<ShipmentResponse>> GetByOrderAsync(Guid orderId);
        Task<Result<ShipmentResponse>> UpdateShipmentAsync(Guid shipmentId, UpdateShipmentRequest request);
        Task<Result<ShipmentResponse>> MarkDeliveredAsync(Guid shipmentId);
    }
}
