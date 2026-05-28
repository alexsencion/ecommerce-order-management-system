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
    public class CreateCategoryValidatorTests
    {
        private readonly CreateCategoryValidator _validator = new();

        [Fact]
        public async Task Validate_ValidRequest_Passes()
        {
            var result = await _validator.ValidateAsync(
                new CreateCategoryRequest { Name = "Electronics", Slug = "electronics" });
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("UPPERCASE")]
        [InlineData("has space")]
        [InlineData("special@char")]
        public async Task Validate_InvalidSlug_Fails(string slug)
        {
            var result = await _validator.ValidateAsync(
                new CreateCategoryRequest { Name="Test", Slug = slug });
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Slug");
        }

        [Fact]
        public async Task Validate_EmptyName_Fails()
        {
            var result = await _validator.ValidateAsync(
                new CreateCategoryRequest { Name = "", Slug = "valid-slug" });
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }
    }
}
