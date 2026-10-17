using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.TestHelpers.Builders;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Domain
{
    public class InvoiceTests
    {
        private static readonly DateTime IssuedAt =
            new(2026, 10, 10, 15, 0, 0, DateTimeKind.Utc);

        private static Order OrderWithStatus(OrderStatus status)
        {
            var order = new OrderBuilder().WithStatus(status).Build();
            order.Customer = new CustomerBuilder().Build();
            return order;
        }

        [Theory]
        [InlineData(OrderStatus.Processing)]
        [InlineData(OrderStatus.Packed)]
        [InlineData(OrderStatus.Shipped)]
        [InlineData(OrderStatus.Delivered)]
        public void Create_PaidOrder_SnapshotsBillingAndAmounts(OrderStatus status)
        {
            var order = OrderWithStatus(status);

            var invoice = Invoice.Create(order, 1, IssuedAt);

            invoice.OrderId.Should().Be(order.Id);
            invoice.BillToName.Should().Be(order.Customer.FullName);
            invoice.BillToEmail.Should().Be(order.Customer.Email);
            invoice.Subtotal.Should().Be(order.Subtotal);
            invoice.Tax.Should().Be(order.Tax);
            invoice.Total.Should().Be(order.Total);
            invoice.IssuedAt.Should().Be(IssuedAt);
        }

        [Theory]
        [InlineData(OrderStatus.Pending)]
        [InlineData(OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Cancelled)]
        public void Create_UnpaidOrder_Throws(OrderStatus status)
        {
            var act = () => Invoice.Create(OrderWithStatus(status), 1, IssuedAt);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*paid orders*");
        }

        [Fact]
        public void FormatNumber_PadsSequenceAndIncludesYearMonth()
        {
            Invoice.FormatNumber(IssuedAt, 7).Should().Be("INV-202610-00007");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void FormatNumber_SequenceBelowOne_Throws(int sequence)
        {
            var act = () => Invoice.FormatNumber(IssuedAt, sequence);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Create_SetsPartitionedStoragePathAndFileName()
        {
            var invoice = Invoice.Create(OrderWithStatus(OrderStatus.Delivered), 3, IssuedAt);

            invoice.InvoiceNumber.Should().Be("INV-202610-00003");
            invoice.StoragePath.Should().Be("invoices/2026/10/INV-202610-00003.pdf");
            invoice.FileName.Should().Be("INV-202610-00003.pdf");
        }
    }
}
