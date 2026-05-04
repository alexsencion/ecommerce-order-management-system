using ECommerce.Application.DTOs.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Validators
{
    public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
    {
        public UpdateCustomerValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Phone)
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .WithMessage("Phone must be in E.164 format.")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            When(x => x.Address != null, () =>
            {
                RuleFor(x => x.Address!.Street).NotEmpty().MaximumLength(200);
                RuleFor(x => x.Address!.City).NotEmpty().MaximumLength(100);
                RuleFor(x => x.Address!.ZipCode).NotEmpty().MaximumLength(20);
                RuleFor(x => x.Address!.Country).NotEmpty().MaximumLength(100);
            });
        }
    }
}
