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
    public class CreateShipmentValidatorTests
    {
        private readonly CreateShipmentValidator _validator = new();

        private CreateShipmentRequest ValidRequest() => new()
        {
            OrderId = Guid.NewGuid(),
            Carrier = "FedEx",
            TrackingNumber = "FEDEX-123456",
            Notes = "Handle with care"
        };

        [Fact]
        public async Task Validate_ValidRequest_Passes()
        {
            var result = await _validator.ValidateAsync(ValidRequest());
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Validate_EmptyCarrier_Fails()
        {
            var req = ValidRequest();
            req.Carrier = "";
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Carrier");
        }

        [Fact]
        public async Task Validate_EmptyTracking_Fails()
        {
            var req = ValidRequest();
            req.TrackingNumber = "";
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "TrackingNumber");
        }

        [Theory]
        [InlineData("lowercase")]
        [InlineData("has space")]
        [InlineData("special@char")]
        public async Task Validate_InvalidTrackingFormat_Fails(string tracking)
        {
            var req = ValidRequest();
            req.TrackingNumber = tracking;
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "TrackingNumber");
        }

        [Theory]
        [InlineData("FEDEX123")]
        [InlineData("UPS-1Z999AA10123456784")]
        [InlineData("DHL-EXPRESS-001")]
        public async Task Validate_ValidTrackingFormats_Pass(string tracking)
        {
            var req = ValidRequest();
            req.TrackingNumber = tracking;
            var result = await _validator.ValidateAsync(req);
            result.Errors.Should().NotContain(e => e.PropertyName == "TrackingNumber");
        }

        [Fact]
        public async Task Validate_NullNotes_Passes()
        {
            var req = ValidRequest();
            req.Notes = null;
            var result = await _validator.ValidateAsync(req);
            result.IsValid.Should().BeTrue();
        }
    }
}
