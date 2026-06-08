using ECommerce.Application.DTOs.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Validators
{
    public class AdjustStockValidator : AbstractValidator<AdjustStockRequest>
    {
        public AdjustStockValidator()
        {
            RuleFor(x => x.Quantity)
                .NotEqual(0).WithMessage("Adjustment quantity cannot be zero.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("A reason for stock adjustment is required.")
                .MaximumLength(500);
        }
    }
}
