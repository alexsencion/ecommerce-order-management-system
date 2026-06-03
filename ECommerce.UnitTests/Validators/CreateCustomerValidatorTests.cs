using ECommerce.Application.DTOs.Request;
using ECommerce.Application.Validators;
using ECommerce.TestHelpers.Builders;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Validators
{
    public class CreateCustomerValidatorTests
    {
        private readonly CreateCustomerValidator _validator = new();

        [Fact]
        public async Task Validate_ValidRequest_Passes()
        {
            var request = new CreateCustomerRequestBuilder().Build();
            var result = await _validator.ValidateAsync(request);
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task Validate_EmptyFirstName_Fails(string? firstName)
        {
            var request = new CreateCustomerRequestBuilder()
                .WithFirstName(firstName ?? "").Build();
            var result = await _validator.ValidateAsync(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task Validate_InvalidEmail_Fails(string email)
        {
            var request = new CreateCustomerRequestBuilder().WithEmail(email).Build();
            var result = await _validator.ValidateAsync(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Email");
        }

        [Fact]
        public async Task Validate_ValidEmail_Passes()
        {
            var request = new CreateCustomerRequestBuilder()
                .WithEmail("valid@example.com").Build();
            var result = await _validator.ValidateAsync(request);
            result.Errors.Should().NotContain(e => e.PropertyName == "Email");
        }

        [Theory]
        [InlineData("not-a-phone")]
        [InlineData("123")]
        [InlineData("00000000000")]
        public async Task Validate_InvalidPhone_Fails(string phone)
        {
            var request = new CreateCustomerRequestBuilder().WithPhone(phone).Build();
            var result = await _validator.ValidateAsync(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Phone");
        }

        [Fact]
        public async Task Validate_NullPhone_Passes()
        {
            var request = new CreateCustomerRequestBuilder().WithoutPhone().Build();
            var result = await _validator.ValidateAsync(request);
            result.Errors.Should().NotContain(e => e.PropertyName == "Phone");
        }

        [Fact]
        public async Task Validate_PartialAddress_Fails()
        {
            var request = new CreateCustomerRequestBuilder()
                .WithAddress(new AddressRequest
                {
                    Street = "123 Main St",
                    City = "",
                    State = "NY",
                    ZipCode = "10001",
                    Country = "US"
                }).Build();

            var result = await _validator.ValidateAsync(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName.Contains("City"));
        }
    }
}
