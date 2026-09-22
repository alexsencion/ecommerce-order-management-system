using ECommerce.Application.DTOs.Request;
using ECommerce.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShipmentsController : BaseApiController
    {
        private readonly IShippingService _shippingService;
        private readonly IValidator<CreateShipmentRequest> _createValidator;

        private readonly IValidator<UpdateShipmentRequest> _updateValidator;

        public ShipmentsController(
            IShippingService shippingService,
            IValidator<CreateShipmentRequest> createValidator,
            IValidator<UpdateShipmentRequest> updateValidator
            )
        {
            _shippingService = shippingService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet("order/{orderId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByOrder(Guid orderId)
        {
            var result = await _shippingService.GetByOrderAsync(orderId);
            return ToResponse(result);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateShipmentRequest request)
        {
            var validation = await _createValidator.ValidateAsync(request);
            if (!validation.IsValid)
                return BadRequest(new
                {
                    errors = validation.Errors.Select(e => e.ErrorMessage)
                });

            var result = await _shippingService.CreateShipmentAsync(request);
            if (!result.IsSuccess) return ToResponse(result);

            return CreatedAtAction(
                nameof(GetByOrder),
                new { orderId = result.Value!.OrderId }, result.Value);
        }

        [HttpPut("{shipmentId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid shipmentId, [FromBody] UpdateShipmentRequest request)
        {
            var validation = await _updateValidator.ValidateAsync(request);
            if (!validation.IsValid)
                return BadRequest(new
                {
                    errors = validation.Errors.Select(e => e.ErrorMessage)
                });

            var result = await _shippingService.UpdateShipmentAsync(shipmentId, request);
            return ToResponse(result);
        }

        [HttpPatch("{shipmentId:guid}/delivered")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkDelivered(Guid shipmentId)
        {
            var result = await _shippingService.MarkDeliveredAsync(shipmentId);
            return ToResponse(result);
        }
    }
}
