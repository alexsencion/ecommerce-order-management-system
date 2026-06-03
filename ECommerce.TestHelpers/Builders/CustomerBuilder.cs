using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.TestHelpers.Builders
{
    public class CustomerBuilder
    {
        private string _firstName = "Jane";
        private string _lastName = "Doe";
        private string _email = $"jane.doe{Guid.NewGuid()}@example.com";
        private string? _phone = "+18095551234";
        private bool _isActive = true;

        public CustomerBuilder WithFirstName(string firstName) { _firstName = firstName; return this; }
        public CustomerBuilder WithLastName(string lastName) { _lastName = lastName; return this; }
        public CustomerBuilder WithEmail(string email) { _email = email; return this; }
        public CustomerBuilder WithPhone(string? phone) { _phone = phone; return this; }
        public CustomerBuilder AsInactive() { _isActive = false; return this; }

        public Customer Build() => new()
        {
            FirstName = _firstName,
            LastName = _lastName,
            Email = _email,
            Phone = _phone,
            IsActive = _isActive
        };

        public static List<Customer> BuildMany(int count) =>
            Enumerable.Range(1, count).Select(i =>
                new CustomerBuilder()
                    .WithEmail($"user{i}@example.com")
                    .WithFirstName($"User{i}")
                    .Build()
            ).ToList();
    }
}
