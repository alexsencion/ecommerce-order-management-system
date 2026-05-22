using ECommerce.Domain.Entities;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Domain
{
    public class OrderItemTests
    {
        [Theory]
        [InlineData(2, 10.00, 20.00)]
        [InlineData(1, 99.99, 99.99)]
        [InlineData(3, 33.33, 99.99)]
        public void LineTotal_IsQuantityTimesUnitPrice(
            int quantity, decimal unitPrice, decimal expected)
        {
            var item = new OrderItem { Quantity = quantity, UnitPrice = unitPrice };
            item.LineTotal.Should().Be(expected);
        }
    }
}
