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
    public class CreateProductValidatorTests
    {
        private readonly CreateProductValidator _validator = new();

        private CreateProductRequest ValidRequest() => new()
        {
            CategoryId = Guid.NewGuid(),
            Name = "Test Product",
            Sku = "SKU-001",
            Price = 29.99m,
            StockQuantity = 10
        };

        [Fact]
        public async Task Validate_ValidRequest_Passes()
        {
            var result = await _validator.ValidateAsync(ValidRequest());
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-0.01)]
        public async Task Validate_PriceNotPositive_Fails(decimal price)
        {
            var req = ValidRequest();
            req.Price = price;
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e  => e.PropertyName == "Price");
        }

        [Theory]
        [InlineData("sku-lowercase")]
        [InlineData("SKU 001")]
        [InlineData("SKU@001")]
        public async Task Validate_InvalidSkuFormat_Fails(string sku)
        {
            var req = ValidRequest();
            req.Sku = sku;
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Sku");
        }

        [Fact]
        public async Task Validate_NegativeStock_Fails()
        {
            var req = ValidRequest();
            req.StockQuantity = -1;
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "StockQuantity");
        }

        [Fact]
        public async Task Validate_InvalidImageUrl_Fails()
        {
            var req = ValidRequest();
            req.ImageUrl = "not-a-url";
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "ImageUrl");
        }

        [Fact]
        public async Task Validate_NullImageUrl_Passes()
        {
            var req = ValidRequest();
            req.ImageUrl = null;
            var result = await _validator.ValidateAsync(req);
            result.Errors.Should().NotContain(e => e.PropertyName == "ImageUrl");
        }
    }
}
