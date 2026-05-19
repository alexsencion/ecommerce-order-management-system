using ECommerce.Application.DTOs.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Validators
{
    public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
    {
        public CreateCustomerValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

            RuleFor(x => x.Phone)
                .Matches(@"^\+[1-9]\d{6,14}$")
                .WithMessage("Phone number must be in E.164 format (e.g. +18095551234).")
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
