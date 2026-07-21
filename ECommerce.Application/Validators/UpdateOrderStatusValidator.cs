using ECommerce.Application.DTOs.Request;
using ECommerce.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Validators
{
    public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusRequest>
    {
        public UpdateOrderStatusValidator()
        {
            RuleFor(x => x.NewStatus)
                .IsInEnum().WithMessage("Invalid order status value.")
                .NotEqual(OrderStatus.Pending)
                .WithMessage("Cannot manually set status back to Pending.");
        }
    }
}
