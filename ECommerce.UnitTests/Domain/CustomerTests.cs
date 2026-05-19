using ECommerce.Domain.Entities;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Domain
{
    public class CustomerTests
    {
        [Theory]
        [InlineData("Jane", "Doe", "Jane Doe")]
        [InlineData("María", "López", "María López")]
        [InlineData("A", "B", "A B")]
        public void FullName_ConcatenatesFirstAndLastName(
            string firstName, string lastName, string expected)
        {
            var customer = new Customer { FirstName = firstName, LastName = lastName };
            customer.FullName.Should().Be(expected);
        }
    }
}
