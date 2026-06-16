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
    public class CreateOrderValidatorTests
    {
        private readonly CreateOrderValidator _validator = new();

        private CreateOrderRequest ValidRequest() => new()
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 2 },
            },
            ShippingAddress = new ShippingAddressRequest
            {
                Street = "123 Main St",
                City = "Santo Domingo",
                State = "DN",
                ZipCode = "10101",
                Country = "DO"
            }
        };

        [Fact]
        public async Task Validate_ValidRequest_Passes()
        {
            var result = await _validator.ValidateAsync(ValidRequest());
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Validate_EmptyItems_Fails()
        {
            var req = ValidRequest();
            req.Items = new();
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Items");
        }

        [Fact]
        public async Task Validate_DuplicateProducts_Fails()
        {
            var productId = Guid.NewGuid();
            var req = ValidRequest();
            req.Items = new()
            {
                new() { ProductId = productId, Quantity = 1 },
                new() { ProductId = productId, Quantity = 2 },
            };
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Duplicate"));
        }

        [Fact]
        public async Task Validate_ZeroItemsQuantity_Fails()
        {
            var req = ValidRequest();
            req.Items[0].Quantity = 0;
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Validate_MissingShippingStreet_Fails()
        {
            var req = ValidRequest();
            req.ShippingAddress.Street = "";
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName.Contains("Street"));
        }

        [Fact]
        public async Task Validate_TooManyItems_Fails()
        {
            var req = ValidRequest();
            req.Items = Enumerable.Range(1, 51)
                .Select(_ => new OrderItemRequest
                    { ProductId = Guid.NewGuid(), Quantity = 1 })
                .ToList();
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("50"));
        }
    }
}
