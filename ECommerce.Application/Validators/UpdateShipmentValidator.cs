using ECommerce.Application.DTOs.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Validators
{
    public class UpdateShipmentValidator : AbstractValidator<UpdateShipmentRequest>
    {
        public UpdateShipmentValidator()
        {
            RuleFor(x => x.Carrier)
                .NotEmpty().WithMessage("Order ID is required.")
                .MaximumLength(100);

            RuleFor(x => x.TrackingNumber)
                .NotEmpty().WithMessage("Tracking number is required.")
                .MaximumLength(200)
                .Matches(@"^[A-Z0-9\-]+$")
                .WithMessage("Tracking number must contain only uppercase letters, numbers, and hyphens.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .When(x => x.Notes != null);
        }
    }
}
