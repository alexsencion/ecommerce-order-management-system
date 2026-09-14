using ECommerce.Application.DTOs.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Validators
{
    public class CreateShipmentValidator : AbstractValidator<CreateShipmentRequest>
    {
        public CreateShipmentValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("Order ID is required.");

            RuleFor(x => x.Carrier)
                .NotEmpty().WithMessage("Carrier name is required.")
                .MaximumLength(100).WithMessage("Carrier must not exceed 100 characters.");

            RuleFor(x => x.TrackingNumber)
                .NotEmpty().WithMessage("Tracking number is required")
                .MaximumLength(200).WithMessage("Carrier must not exceed 200 characters.")
                .Matches(@"^[A-Z0-9\-]+$")
                .WithMessage("Tracking number must contain only uppercase letters, numbers, and hyphens.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes must not exceed 500 characters")
                .When(x => x.Notes != null);
        }
    }
}
