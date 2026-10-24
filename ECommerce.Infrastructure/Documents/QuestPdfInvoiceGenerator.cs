using ECommerce.Application.Common;
using ECommerce.Application.Models;
using ECommerce.Application.Services;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Documents
{
    public class QuestPdfInvoiceGenerator : IInvoicePdfGenerator
    {
        private readonly InvoiceSettings _settings;

        public QuestPdfInvoiceGenerator(IOptions<InvoiceSettings> settings)
        {
            _settings = settings.Value;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] Generate(InvoiceDocument m) =>
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, m));
                    page.Content().PaddingVertical(20).Element(c => ComposeContent(c, m));
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            }).GeneratePdf();

        private void ComposeHeader(IContainer container, InvoiceDocument m)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(_settings.CompanyName).FontSize(18).SemiBold();
                    col.Item().Text(_settings.CompanyAddress);
                    col.Item().Text(_settings.CompanyEmail);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().AlignRight().Text("INVOICE").FontSize(20).Bold();
                    col.Item().AlignRight().Text($"No. {m.InvoiceNumber}");
                    col.Item().AlignRight().Text($"Issued: {m.IssuedAt:yyyy-MM-dd}");
                    col.Item().AlignRight().Text($"Order: #{m.OrderReference}");
                });
            });
        }

        private void ComposeContent(IContainer container, InvoiceDocument m)
        {
            container.Column(col =>
            {
                col.Spacing(20);

                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("BILL TO").SemiBold();
                        c.Item().Text(m.BillToName);
                        c.Item().Text(m.BillToEmail);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("SHIP TO").SemiBold();
                        c.Item().Text(m.ShipTo.Street);
                        c.Item().Text($"{m.ShipTo.City}, {m.ShipTo.State} {m.ShipTo.ZipCode}");
                        c.Item().Text(m.ShipTo.Country);
                    });
                });

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                        cols.ConstantColumn(40);
                        cols.ConstantColumn(70);
                        cols.ConstantColumn(80);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text("Product");
                        h.Cell().Element(HeaderCell).Text("SKU");
                        h.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                        h.Cell().Element(HeaderCell).AlignRight().Text("Unit");
                        h.Cell().Element(HeaderCell).AlignRight().Text("Total");
                    });

                    foreach (var line in m.Lines)
                    {
                        table.Cell().Element(BodyCell).Text(line.Description);
                        table.Cell().Element(BodyCell).Text(line.Sku);
                        table.Cell().Element(BodyCell).AlignRight().Text(line.Quantity.ToString());
                        table.Cell().Element(BodyCell).AlignRight().Text(Money(line.UnitPrice));
                        table.Cell().Element(BodyCell).AlignRight().Text(Money(line.LineTotal));
                    }
                });

                col.Item().AlignRight().Width(220).Column(t =>
                {
                    TotalRow(t, "Subtotal", m.Subtotal);
                    TotalRow(t, $"Tax ({_settings.TaxRateLabel})", m.Tax);
                    TotalRow(t, "Total", m.Total, bold: true);
                });
            });
        }

        private static IContainer HeaderCell(IContainer c) =>
            c.BorderBottom(1).BorderColor(Colors.Grey.Medium)
            .PaddingVertical(5).DefaultTextStyle(x => x.SemiBold());

        private static IContainer BodyCell(IContainer c) =>
            c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4);

        private static void TotalRow(ColumnDescriptor col, string label, decimal amount, bool bold = false)
        {
            col.Item().PaddingVertical(2).Row(r =>
            {
                var l = r.RelativeItem().Text(label);
                var v = r.RelativeItem().AlignRight().Text(Money(amount));
                if (bold) { l.Bold(); v.Bold(); }
            });
        }

        private static string Money(decimal value) =>
            "$" + value.ToString("N2", CultureInfo.InvariantCulture);
    }
}
