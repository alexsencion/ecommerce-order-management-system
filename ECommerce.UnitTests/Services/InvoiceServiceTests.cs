using AutoMapper;
using ECommerce.Application.Models;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces.Repositories;
using ECommerce.TestHelpers.Builders;
using ECommerce.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Services
{
    public class InvoiceServiceTests : BaseTest
    {
        private static readonly DateTimeOffset Now = 
            new(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);

        private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
        private readonly Mock<IInvoicePdfGenerator> _pdf = new();
        private readonly Mock<IFileStorage> _storage = new();

        private readonly InvoiceService _sut;

        public InvoiceServiceTests() : base()
        {
            _uowMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
            _uowMock.Setup(u => u.Invoices).Returns(_invoiceRepo.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _invoiceRepo.Setup(r => r.AddAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);
            _invoiceRepo.Setup(r => r.CountIssuedInMonthAsync(2026, 10)).ReturnsAsync(0);

            _pdf.Setup(p => p.Generate(It.IsAny<InvoiceDocument>()))
                .Returns(new byte[] { 0x25, 0x50, 0x44, 0x46 });

            _sut = new InvoiceService(
                _uowMock.Object, _pdf.Object, _storage.Object, _mapper, 
                new FixedTimeProvider(Now), new Mock<ILogger<InvoiceService>>().Object);
        }

        private static Order PaidOrder(OrderStatus status = OrderStatus.Delivered)
        {
            var product = new ProductBuilder().Build();
            var order = new OrderBuilder()
                .WithStatus(status)
                .WithItems(new List<OrderItem>
                {
                    new() { ProductId = product.Id, Product = product, Quantity = 2, UnitPrice = 50m }
                })
                .Build();
            order.Customer = new CustomerBuilder().Build();
            order.ShippingAddressJson = JsonSerializer.Serialize(new
            {
                street = "123 Main St",
                city = "Santo Domingo",
                state = "DN",
                zipCode = "10101",
                country = "DO"
            });
            return order;
        }

        [Fact]
        public async Task Generate_PaidOrder_CreatesInvoiceStoresPdfAndReturns201()
        {
            var order = PaidOrder();
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);

            var result = await _sut.GenerateAsync(order.Id);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Value!.InvoiceNumber.Should().Be("INV-202610-00001");
            result.Value!.FileName.Should().Be("INV-202610-00001.pdf");

            _pdf.Verify(p => p.Generate(It.Is<InvoiceDocument>(d =>
                d.InvoiceNumber == "INV-202610-00001" &&
                d.Lines.Count == 1 &&
                d.ShipTo.City == "Santo Domingo")), Times.Once);

            _storage.Verify(s => s.SaveAsync(
                "invoices/2026/10/INV-202610-00001.pdf",
                It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);

            _invoiceRepo.Verify(r => r.AddAsync(It.IsAny<Invoice>()), Times.Once);
        }

        [Fact]
        public async Task Generate_ExistingInvoicesThisMonth_IncrementsSequence()
        {
            var order = PaidOrder();
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);
            _invoiceRepo.Setup(r => r.CountIssuedInMonthAsync(2026, 10)).ReturnsAsync(4);

            var result = await _sut.GenerateAsync(order.Id);

            result.Value!.InvoiceNumber.Should().Be("INV-202610-00005");
        }

        [Fact]
        public async Task Generate_InvoiceAlreadyExists_ReturnsExistingWithoutRegenerating()
        {
            var order = PaidOrder();
            var existing = Invoice.Create(order, 1, Now.UtcDateTime);
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);
            _invoiceRepo.Setup(r => r.GetByOrderIdAsync(order.Id)).ReturnsAsync(existing);

            var result = await _sut.GenerateAsync(order.Id);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Value!.Id.Should().Be(existing.Id);
            _pdf.Verify(p => p.Generate(It.IsAny<InvoiceDocument>()), Times.Never);
            _invoiceRepo.Verify(r => r.AddAsync(It.IsAny<Invoice>()), Times.Never);
        }

        [Theory]
        [InlineData(OrderStatus.Pending)]
        [InlineData(OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Cancelled)]
        public async Task Generate_UnPaidOrder_Returns400AndGeneratesNothing(OrderStatus status)
        {
            var order = PaidOrder(status);
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);

            var result = await _sut.GenerateAsync(order.Id);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            _pdf.Verify(p => p.Generate(It.IsAny<InvoiceDocument>()), Times.Never);
            _storage.Verify(s => s.SaveAsync(
                It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Generate_OrderNotFound_Returns404()
        {
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>()))
                          .ReturnsAsync((Order?)null);

            var result = await _sut.GenerateAsync(Guid.NewGuid());

            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Generate_DatabaseSaveFails_DeletesStoredPdfAndRethrows()
        {
            var order = PaidOrder();
            _orderRepoMock.Setup(r => r.GetWithDetailsAsync(order.Id)).ReturnsAsync(order);
            _uowMock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new Exception("db down"));

            var act = () => _sut.GenerateAsync(order.Id);

            await act.Should().ThrowAsync<Exception>().WithMessage("db down");
            _storage.Verify(s => s.DeleteAsync(
                "invoices/2026/10/INV-202610-00001.pdf",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByOrder_NoInvoice_Returns404()
        {
            var result = await _sut.GetByOrderAsync(Guid.NewGuid());
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Download_ExistingInvoice_ReturnsPdfBytesAndFileName()
        {
            var invoice = Invoice.Create(PaidOrder(), 1, Now.UtcDateTime);
            var bytes = new byte[] { 1, 2, 3 };
            _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
            _storage.Setup(s => s.ReadAsync(invoice.StoragePath, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(bytes);

            var result = await _sut.DownloadAsync(invoice.Id);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Content.Should().Equal(bytes);
            result.Value.FileName.Should().Be("INV-202610-00001.pdf");
            result.Value.ContentType.Should().Be("application/pdf");
        }

        [Fact]
        public async Task Download_FileMissingFromStorage_Returns404()
        {
            var invoice = Invoice.Create(PaidOrder(), 1, Now.UtcDateTime);
            _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
            _storage.Setup(s => s.ReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?) null);

            var result = await _sut.DownloadAsync(invoice.Id);

            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Download_InvoiceNotFound_Returns404()
        {
            _invoiceRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                        .ReturnsAsync((Invoice?)null);

            var result = await _sut.DownloadAsync(Guid.NewGuid());

            result.StatusCode.Should().Be(404);
        }
    }
}
