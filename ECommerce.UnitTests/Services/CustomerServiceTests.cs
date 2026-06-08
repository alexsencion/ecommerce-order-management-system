using AutoMapper;
using Castle.Core.Logging;
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
    public class CustomerServiceTests : BaseTest
    {
        private readonly Mock<ICustomerRepository> _customerRepMock;
        private readonly CustomerService _sut;

        public CustomerServiceTests() : base()
        {
            _customerRepMock = new Mock<ICustomerRepository>();

            _uowMock.Setup(u => u.Customers).Returns(_customerRepMock.Object);

            _sut = new CustomerService(_uowMock.Object, _mapper, _loggerCustomerMock.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingCustomer_ReturnsSuccess()
        {
            var customer = new CustomerBuilder().WithEmail("test@example.com").Build();
            _customerRepMock.Setup(r => r.GetByIdAsync(customer.Id))
                            .ReturnsAsync(customer);

            var result = await _sut.GetByIdAsync(customer.Id);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Email.Should().Be("test@example.com");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentId_ReturnsNotFound()
        {
            _customerRepMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                            .ReturnsAsync((Customer?)null);

            var result = await _sut.GetByIdAsync(Guid.NewGuid());

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreateAsync_NewEmail_CreatesAndReturnsCustomer()
        {
            var request = new CreateCustomerRequestBuilder().Build();

            _customerRepMock.Setup(r => r.EmailExistsAsync(request.Email))
                            .ReturnsAsync(false);
            _customerRepMock.Setup(r => r.AddAsync(It.IsAny<Customer>()))
                            .Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.CreateAsync(request);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Value!.Email.Should().Be(request.Email.ToLower().Trim());
            _customerRepMock.Verify(r => r.AddAsync(It.IsAny<Customer>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_DuplicateEmail_ReturnsConflict()
        {
            var request = new CreateCustomerRequestBuilder().Build();
            _customerRepMock.Setup(r => r.EmailExistsAsync(request.Email))
                            .ReturnsAsync(true);

            var result = await _sut.CreateAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            _customerRepMock.Verify(r => r.AddAsync(It.IsAny<Customer>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ExistingCustomer_UpdatesAndReturnsSuccess()
        {
            var customer = new CustomerBuilder().Build();
            var request = new UpdateCustomerRequest
            {
                FirstName = "Updated",
                LastName = "Name",
                Phone = "+18095559999"
            };

            _customerRepMock.Setup(r => r.GetByIdAsync(customer.Id)).ReturnsAsync(customer);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.UpdateAsync(customer.Id, request);

            result.IsSuccess.Should().BeTrue();
            result.Value!.FirstName.Should().Be("Updated");
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NonExistentCustomer_ReturnsNotFound()
        {
            _customerRepMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                            .ReturnsAsync((Customer?)null);

            var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateCustomerRequest
            {
                FirstName = "X",
                LastName = "X",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task DeactivateAsync_ActiveCustomer_DeactivatesAndReturnsSuccess()
        {
            var customer = new CustomerBuilder().Build();
            _customerRepMock.Setup(r => r.GetByIdAsync(customer.Id)).ReturnsAsync(customer);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.DeactivateAsync(customer.Id);

            result.IsSuccess.Should().BeTrue();
            customer.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task DeactivateAsync_AlreadyInactiveCustomer_ReturnsFailure()
        {
            var customer = new CustomerBuilder().AsInactive().Build();
            _customerRepMock.Setup(r => r.GetByIdAsync(customer.Id)).ReturnsAsync(customer);

            var result = await _sut.DeactivateAsync(customer.Id);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            _uowMock.Verify(u =>  u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeactivateAsync_NonExistentCustomer_ReturnsNotFound()
        {
            _customerRepMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                            .ReturnsAsync((Customer?)null);

            var result = await _sut.DeactivateAsync(Guid.NewGuid());

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task DeleteAsync_ExistingCustomer_DeletesAndReturnsSuccess()
        {
            var customer = new CustomerBuilder().Build();
            _customerRepMock.Setup(r => r.GetByIdAsync(customer.Id)).ReturnsAsync(customer);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sut.DeleteAsync(customer.Id);

            result.IsSuccess!.Should().BeTrue();
            _customerRepMock.Verify(r => r.Remove(customer), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistentCustomer_ReturnsNotFound()
        {
            _customerRepMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                            .ReturnsAsync((Customer?)null);

            var result = await _sut.DeleteAsync(Guid.NewGuid());

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetAllAsync_WithSearch_FiltersResults()
        {
            var customers = new List<Customer>
            {
                new CustomerBuilder().WithFirstName("Alice").WithEmail("alice@example.com").Build(),
                new CustomerBuilder().WithFirstName("Bob").WithEmail("bob@example.com").Build(),
            };
            _customerRepMock.Setup(r => r.GetAllAsync()).ReturnsAsync(customers);

            var result = await _sut.GetAllAsync(new() { Search = "alice" });

            result.IsSuccess.Should().BeTrue();
            result.Value!.Data.Should().HaveCount(1);
            result.Value.Data.First().FirstName.Should().Be("Alice");
        }

        [Fact]
        public async Task GetAllAsync_PaginationRespectsPageSize()
        {
            var customers = CustomerBuilder.BuildMany(25);
            _customerRepMock.Setup(r => r.GetAllAsync()).ReturnsAsync(customers);

            var result = await _sut.GetAllAsync(new() { Page = 1, PageSize = 10 });

            result.IsSuccess.Should().BeTrue();
            result.Value!.Data.Should().HaveCount(10);
            result.Value.TotalCount.Should().Be(25);
            result.Value.TotalPages.Should().Be(3);
        }

        [Fact]
        public async Task GetAllAsync_PageSizeExceedsCap_ClamspsTo50()
        {
            var customers = CustomerBuilder.BuildMany(60);
            _customerRepMock.Setup(r => r.GetAllAsync()).ReturnsAsync(customers);

            var result = await _sut.GetAllAsync(new() { Page = 1, PageSize = 999 });

            result.Value!.Data.Should().HaveCount(50);
        }

    }
}
