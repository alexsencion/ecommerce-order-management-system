using ECommerce.Application.DTOs.Request;
using ECommerce.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : BaseApiController
    {
        private readonly ICustomerService _customerService;
        private readonly IValidator<CreateCustomerRequest> _createValidator;
        private readonly IValidator<UpdateCustomerRequest> _updateValidator;

        public CustomersController(
            ICustomerService customerService,
            IValidator<CreateCustomerRequest> createValidator,
            IValidator<UpdateCustomerRequest> updateValidator)
        {
            _customerService = customerService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] CustomerQueryParams query)
        {
            var result = await _customerService.GetAllAsync(query);
            return ToResponse(result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _customerService.GetByIdAsync(id);
            return ToResponse(result);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
        {
            var validation = await _createValidator.ValidateAsync(request);
            if (!validation.IsValid)
                return BadRequest(new { errors = validation.Errors  .Select(e => e.ErrorMessage) });

            var result = await _customerService.CreateAsync(request);
            if (!result.IsSuccess)
                return ToResponse(result);

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request)
        {
            var validation = await _updateValidator.ValidateAsync(request);
            if (!validation.IsValid)
                return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

            var result = await _customerService.UpdateAsync(id, request);
            return ToResponse(result);
        }

        [HttpPatch("{id:guid}/deactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var result = await _customerService.DeactivateAsync(id);
            return ToResponse(result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _customerService.DeleteAsync(id);
            return ToResponse(result);
        }
    }
}
