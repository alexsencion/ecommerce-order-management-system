using ECommerce.Domain.Common;
using ECommerce.TestHelpers.Builders;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Domain
{
    public class StockReservationTests
    {
        [Fact]
        public void Reserve_SufficientStock_IncreasesReserved()
        {
            var product = new ProductBuilder().WithStock(100).WithReserved(0).Build();

            StockReservation.Reserve(product, 30);

            product.ReservedQuantity.Should().Be(30);
            product.StockQuantity.Should().Be(100);
            product.AvailableStock.Should().Be(70);
        }

        [Fact]
        public void Reserve_Insufficient_Throws()
        {
            var product = new ProductBuilder().WithStock(5).WithReserved(3).Build();

            var act = () => StockReservation.Reserve(product, 5);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Insufficient stock*");
        }

        [Fact]
        public void Reserve_ExactlyAvailableStock_Succeeds()
        {
            var product = new ProductBuilder().WithStock(10).WithReserved(0).Build();

            var act = () => StockReservation.Reserve(product, 10);

            act.Should().NotThrow();
            product.AvailableStock.Should ().Be(0);
        }

        [Fact]
        public void Reserve_ZeroQuantity_Throws()
        {
            var product = new ProductBuilder().WithStock(10).Build();
            var act = () => StockReservation.Reserve(product, 0);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Confirm_DeductsStockAndReleasesReservation()
        {
            var product = new ProductBuilder().WithStock(100).WithReserved(30).Build();

            StockReservation.Confirm(product, 30);

            product.StockQuantity.Should().Be(70);
            product.ReservedQuantity.Should().Be(0);
        }

        [Fact]
        public void Confirm_PartialQuantity_OnlyDeductsSpecifiedAmount()
        {
            var product = new ProductBuilder().WithStock(100).WithReserved(50).Build();

            StockReservation.Confirm(product, 20);

            product.StockQuantity.Should().Be(80);
            product.ReservedQuantity.Should().Be(30);
        }

        [Fact]
        public void Release_ClearsReservationWithoutDeductingStock()
        {
            var product = new ProductBuilder().WithStock(100).WithReserved(30).Build();

            StockReservation.Release(product, 30);

            product.ReservedQuantity.Should().Be(0);
            product.StockQuantity.Should().Be(100);
            product.AvailableStock.Should().Be(100);
        }

        [Fact]
        public void Release_NeverGoesNegative()
        {
            var product = new ProductBuilder().WithStock(10).WithReserved(5).Build();

            var act = () => StockReservation.Release(product, 99);

            act.Should().NotThrow();
            product.ReservedQuantity.Should().Be(0);
        }

    }
}
