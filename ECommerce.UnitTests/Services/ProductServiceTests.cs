using AutoMapper;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.Mappings;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces.Repositories;
using ECommerce.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Services
{
    public class ProductServiceTests : BaseTest
    {
        private readonly Mock<IProductRepository> _productRepoMock = new();
        private readonly Mock<ICategoryRepository> _categoryRepoMock = new();

        private readonly ProductService _sut;

        public ProductServiceTests() : base()
        {
            _uowMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
            _uowMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);                  

            _sut = new ProductService(_uowMock.Object, _mapper, _loggerProductMock.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingProduct_ReturnsSuccess()
        {
            var product = new ProductBuilder().WithSku("SKU-TEST").Build();
            _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

            var result = await _sut.GetByIdAsync(product.Id);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Sku.Should().Be("SKU-TEST");
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_Returns404()
        {
            _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                            .ReturnsAsync((Product?)null);

            var result = await _sut.GetByIdAsync(Guid.NewGuid());

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_CreatesProduct()
        {
            var categoryId = Guid.NewGuid();
            var category = new CategoryBuilder().Build();
            var request = new CreateProductRequest
            {
                CategoryId = categoryId,
                Name = "New Product",
                Sku = "NEW-001",
                Price = 49.99m,
                StockQuantity = 50,
            };

            _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
            _productRepoMock.Setup(r => r.GetBySkuAsync("NEW-001")).ReturnsAsync((Product?)null);
            _productRepoMock.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
            _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                            .ReturnsAsync(new ProductBuilder().WithSku("NEW-001").Build());
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.CreateAsync(request);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            _productRepoMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_DuplicateSku_ReturnsConflict()
        {
            var categoryId = Guid.NewGuid();
            _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId))
                             .ReturnsAsync(new CategoryBuilder().Build());
            _productRepoMock.Setup(r => r.GetBySkuAsync("DUP-001"))
                            .ReturnsAsync(new ProductBuilder().Build());

            var result = await _sut.CreateAsync(new CreateProductRequest
            {
                CategoryId = categoryId,
                Name = "X",
                Sku = "DUP-001",
                Price = 1,
                StockQuantity = 0,
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task CreateAsync_InvalidCategory_ReturnsFailure()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                             .ReturnsAsync((Category?)null);

            var result = await _sut.CreateAsync(new CreateProductRequest
            {
                CategoryId = Guid.NewGuid(),
                Name = "X",
                Sku = "SKU-X",
                Price = 1,
                StockQuantity = 0,
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(422);
        }

        [Fact]
        public async Task AdjustStockAsync_PositiveDelta_IncreasesStock()
        {
            var product = new ProductBuilder().WithStock(50).Build();
            _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.AdjustStockAsync(product.Id,
                new AdjustStockRequest { Quantity = 20, Reason = "Restock" });

            result.IsSuccess.Should().BeTrue();
            product.StockQuantity.Should().Be(70);
        }

        [Fact]
        public async Task AdjustStockAsync_NegativeDelta_DecreasesStock()
        {
            var product = new ProductBuilder().WithStock(50).Build();
            _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.AdjustStockAsync(product.Id, new AdjustStockRequest { Quantity = -10, Reason = "Damage" });

            result.IsSuccess.Should().BeTrue();
            product.StockQuantity.Should().Be(40);
        }

        [Fact]
        public async Task AdjustStockAsync_WouldGoBelowZero_ReturnsFailure()
        {
            var product = new ProductBuilder().WithStock(5).Build();
            _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

            var result = await _sut.AdjustStockAsync(product.Id,
                new AdjustStockRequest { Quantity = -10, Reason = "Error" });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeactiveAsync_ActiveProduct_Deactivates()
        {
            var product = new ProductBuilder().Build();
            _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.DeactivateAsync(product.Id);

            result.IsSuccess.Should().BeTrue();
            product.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task DeactivateAsync_AlreadyInactive_ReturnsFailure()
        {
            var product = new ProductBuilder().AsInactive().Build();
            _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

            var result = await _sut.DeactivateAsync(product.Id);

            result?.IsSuccess.Should().BeFalse();
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ProductWithReservations_ReturnsFailure()
        {
            var product = new ProductBuilder().WithReserved(3).Build();
            _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

            var result = await _sut.DeleteAsync(product.Id);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            _productRepoMock.Verify(r => r.Remove(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task GetAllAsync_InStockOnly_ExcludesZeroStock()
        {
            var products = new List<Product>
            {
                new ProductBuilder().WithName("In Stock").WithStock(10).Build(),
                new ProductBuilder().WithName("Out of Stock").WithStock(0).Build()
            };
            _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

            var result = await _sut.GetAllAsync(new ProductQueryParams { InStockOnly = true });

            result.Value!.Data.Should().HaveCount(1);
            result.Value.Data.First().Name.Should().Be("In Stock");
        }

        [Fact]
        public async Task GetAllAsync_PriceRange_FiltersCorrectly()
        {
            var products = new List<Product>
            {
                new ProductBuilder().WithName("Cheap").WithPrice(5m).Build(),
                new ProductBuilder().WithName("Mid").WithPrice(50m).Build(),
                new ProductBuilder().WithName("Expensive").WithPrice(500m).Build()
            };
            _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

            var result = await _sut.GetAllAsync(
                new ProductQueryParams { MinPrice = 10m, MaxPrice = 100m });

            result.Value!.Data.Should().HaveCount(1);
            result.Value.Data.First().Name.Should().Be("Mid");
        }
    }
}
