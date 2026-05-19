using ECommerce.Application.DTOs.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Builders
{
    public class CreateCustomerRequestBuilder
    {
        private string _firstName = "Jane";
        private string _lastName = "Doe";
        private string _email = "jane.doe@example.com";
        private string? _phone = "+18095551234";
        private AddressRequest? _address;

        public CreateCustomerRequestBuilder WithFirstName(string v) { _firstName = v; return this; }
        public CreateCustomerRequestBuilder WithLastName(string v) { _lastName = v; return this; }
        public CreateCustomerRequestBuilder WithEmail(string v) { _email = v; return this; }
        public CreateCustomerRequestBuilder WithPhone(string? v) { _phone = v; return this; }
        public CreateCustomerRequestBuilder WithAddress(AddressRequest a) { _address = a; return this; }
        public CreateCustomerRequestBuilder WithoutPhone() { _phone = null; return this; }

        public CreateCustomerRequest Build() => new()
        {
            FirstName = _firstName,
            LastName = _lastName,
            Email = _email,
            Phone = _phone,
            Address = _address
        };
    }
}
