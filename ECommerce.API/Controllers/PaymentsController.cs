using ECommerce.Application.DTOs.Request;
using ECommerce.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : BaseApiController
    {
        private readonly IPaymentService _paymentService;
        private readonly IValidator<CreatePaymentIntentRequest> _createValidator;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentService paymentService,
            IValidator<CreatePaymentIntentRequest> createValidator,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _createValidator = createValidator;
            _logger = logger;
        }

        [HttpPost("intent")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateIntent(
            [FromBody] CreatePaymentIntentRequest request)
        {
            var validation = await _createValidator.ValidateAsync(request);
            if (!validation.IsValid)
                return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

            var result = await _paymentService.CreatePaymentIntentAsync(request);
            return ToResponse(result);
        }

        [HttpGet("order/{orderId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByOrder(Guid orderId)
        {
             var result = await _paymentService.GetPaymentByOrderAsync(orderId);
            return ToResponse(result);
        }

        [HttpPost("order/{orderId:guid}/refund")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Refund(Guid orderId)
        {
            var result = await _paymentService.RefundAsync(orderId);
            return ToResponse(result);
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Webhook()
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var payload = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            if (!Request.Headers.TryGetValue("Stripe-Signature", out var signature))
            {
                _logger.LogWarning("Webhook received without Stripe-Signature header.");
                return BadRequest(new { error = "Missing Stripe-Signature header." });
            }

            try
            {
                var result = await _paymentService.HandleWebhookAsync(payload, signature!);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Webhook processing failed: {Error}", result.Error);
                    return BadRequest(new { error = result.Error });
                }

                return Ok(new { received = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "webhook exception");
                throw;
            }
        }
    }
}
