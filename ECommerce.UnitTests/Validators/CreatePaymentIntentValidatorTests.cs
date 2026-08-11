using ECommerce.Application.DTOs.Request;
using ECommerce.Application.Validators;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Validators
{
    public class CreatePaymentIntentValidatorTests
    {
        private readonly CreatePaymentIntentValidator _validator = new();

        [Fact]
        public async Task Validate_ValidRequest_Passes()
        {
            var request = new CreatePaymentIntentRequest { OrderId = Guid.NewGuid() };
            var result = await _validator.ValidateAsync(request);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Validate_EmptyOrderId_Fails()
        {
            var request = new CreatePaymentIntentRequest { OrderId = Guid.Empty };
            var result = await _validator.ValidateAsync(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
        }
    }
}
