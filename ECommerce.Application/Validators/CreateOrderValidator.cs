using ECommerce.Application.DTOs.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Validators
{
    public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("Customer is required.");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Order must contain at least one item.")
                .Must(items => items.Count <= 50)
                .WithMessage("An Order cannot contain more than 50 line items.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .NotEmpty().WithMessage("Product ID is required.");
                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage("Item quantity must be at least 1.")
                    .LessThanOrEqualTo(1000).WithMessage("Item quantity cannot exceed 1000.");
            });

            RuleFor(x => x.Items)
                .Must(items =>
                {
                    var ids = items.Select(i => i.ProductId).ToList();
                    return ids.Count == ids.Distinct().Count();
                })
                .WithMessage("Duplicate products in order. Combine quantities instead.");

            RuleFor(x => x.ShippingAddress).NotNull()
                .WithMessage("Shipping address is required.");

            When(x => x.ShippingAddress != null, () =>
            {
                RuleFor(x => x.ShippingAddress.Street)
                    .NotEmpty().MaximumLength(200);
                RuleFor(x => x.ShippingAddress.City)
                    .NotEmpty().MaximumLength(100);
                RuleFor(x => x.ShippingAddress.ZipCode)
                    .NotEmpty().MaximumLength(20);
                RuleFor(x => x.ShippingAddress.Country)
                    .NotEmpty().MaximumLength(100);
            });
        }
    }
}
