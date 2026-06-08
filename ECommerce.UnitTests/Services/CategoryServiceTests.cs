using AutoMapper;
using Castle.Core.Logging;
using ECommerce.Application.DTOs.Request;
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
    public class CategoryServiceTests : BaseTest
    {
        private readonly Mock<ICategoryRepository> _categoryRepoMock = new();

        private readonly CategoryService _sut;

        public CategoryServiceTests() : base()
        {
            _uowMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);

            _sut = new CategoryService(_uowMock.Object, _mapper, _loggerCategoryMock.Object);
        }

        [Fact]
        public async Task CreateAsync_UniqueSlug_CreatesCategory()
        {
            _categoryRepoMock.Setup(r => r.SlugExistsAsync("electronics")).ReturnsAsync(false);
            _categoryRepoMock.Setup(r => r.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.CreateAsync(
                new CreateCategoryRequest { Name = "Electronics", Slug = "electronics" });

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
        }

        [Fact]
        public async Task CreateAsync_DuplicateSlug_ReturnsConflict()
        {
            _categoryRepoMock.Setup(r => r.SlugExistsAsync("electronics")).ReturnsAsync(true);

            var result = await _sut.CreateAsync(
                new CreateCategoryRequest { Name = "Electronics", Slug = "electronics" });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task DeleteAsync_CategoryWithProducts_ReturnsFailure()
        {
            var category = new CategoryBuilder().Build();
            _categoryRepoMock.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
            _categoryRepoMock.Setup(r => r.HasProductsAsync(category.Id)).ReturnsAsync(true);

            var result = await _sut.DeleteAsync(category.Id);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            _categoryRepoMock.Verify(r => r.Remove(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_EmptyCategory_Succeeds()
        {
            var category = new CategoryBuilder().Build();
            _categoryRepoMock.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
            _categoryRepoMock.Setup(r => r.HasProductsAsync(category.Id)).ReturnsAsync(false);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.DeleteAsync(category.Id);

            result.IsSuccess.Should().BeTrue();
            _categoryRepoMock.Verify(r => r.Remove(category), Times.Once);
        }
        
        [Fact]
        public async Task UpdateAsync_SameSlug_DoesNotCheckForConflict()
        {
            var category = new CategoryBuilder().WithSlug("electronics").Build();
            _categoryRepoMock.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.UpdateAsync(category.Id,
                new UpdateCategoryRequest { Name = "Electronics Updated", Slug = "electronics" });

            result.IsSuccess.Should().BeTrue();
            _categoryRepoMock.Verify(r => r.SlugExistsAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
