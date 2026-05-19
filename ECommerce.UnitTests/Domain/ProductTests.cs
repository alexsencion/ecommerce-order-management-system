using ECommerce.Domain.Entities;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Domain
{
    public class ProductTests
    {
        [Fact]
        public void AvailableStock_ReturnsStockMinusReserved()
        {
            var product = new Product { StockQuantity = 100, ReservedQuantity = 30 };
            product.AvailableStock.Should().Be(70);
        }

        [Fact]
        public void AvailableStock_WhenNoReservations_EqualsStockQuantity()
        {
            var product = new Product { StockQuantity = 50, ReservedQuantity = 0 };
            product.AvailableStock.Should().Be(50);
        }

        [Fact]
        public void AvailableStock_WhenFullyReserved_ReturnsZero()
        {
            var product = new Product { StockQuantity = 10, ReservedQuantity = 10 };
            product.AvailableStock.Should().Be(0);
        }
    }
}
