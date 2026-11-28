using ECommerce.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : BaseApiController
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpPost("order/{orderId:guid}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Generate(Guid orderId)
        {
            var result = await _invoiceService.GenerateAsync(orderId);
            if (!result.IsSuccess) return ToResponse(result);

            return result.StatusCode == StatusCodes.Status201Created
                ? CreatedAtAction(nameof(GetByOrder), new { orderId }, result.Value)
                : Ok(result.Value);
        }

        [HttpGet("order/{orderId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByOrder(Guid orderId)
        {
            var result = await _invoiceService.GetByOrderAsync(orderId);
            return ToResponse(result);
        }

        [HttpGet("{invoiceId:guid}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Download(Guid InvoiceId)
        {
            var result = await _invoiceService.DownloadAsync(InvoiceId);
            if (!result.IsSuccess) return ToResponse(result);

            var file = result.Value!;
            return File(file.Content, file.ContentType, file.FileName);
        }
    }
}
