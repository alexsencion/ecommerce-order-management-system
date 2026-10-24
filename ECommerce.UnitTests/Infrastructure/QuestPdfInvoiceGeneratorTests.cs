using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Response;
using ECommerce.Application.Models;
using ECommerce.Infrastructure.Documents;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Infrastructure
{
    public class QuestPdfInvoiceGeneratorTests
    {
        private readonly QuestPdfInvoiceGenerator _sut =
            new(Options.Create(new InvoiceSettings { CompanyName = "Test Co" }));

        private static InvoiceDocument Document(int lineCount) => new(
            InvoiceNumber: "INV-202610-00001",
            IssuedAt: new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            OrderReference: "ABCD1234",
            BillToName: "Jane Doe",
            BillToEmail: "jane@example.com",
            ShipTo: new ShippingAddressDto
            {
                Street = "123 Main St",
                City = "Santo Domingo",
                State = "DN",
                ZipCode = "10101",
                Country = "DO"
            },
            Lines: Enumerable.Range(1, lineCount)
                .Select(i => new InvoiceLine($"Product {i}", $"SKU-{i}", 1, 10m, 10m))
                .ToList(),
            Subtotal: 10m * lineCount,
            Tax: 1.8m * lineCount,
            Total: 11.8m * lineCount);

        [Fact]
        public void Generate_ReturnsValidPdfBytes()
        {
            var bytes = _sut.Generate(Document(lineCount: 3));

            bytes.Should().NotBeEmpty();
            Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
        }

        [Fact]
        public void Generate_ManyLines_PaginatesWithoutError()
        {
            var act = () => _sut.Generate(Document(lineCount: 120));
            act.Should().NotThrow();
        }
    }
}
