using AutoMapper;
using ECommerce.Application.Mappings;
using ECommerce.Application.Services;
using ECommerce.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Services
{
    public abstract class BaseTest
    {
        protected readonly IMapper _mapper;
        protected readonly Mock<IUnitOfWork> _uowMock;
        protected readonly Mock<ILogger<CustomerService>> _loggerCustomerMock;
        protected readonly Mock<ILogger<ProductService>> _loggerProductMock;
        protected readonly Mock<ILogger<CategoryService>> _loggerCategoryMock;
        protected readonly Mock<ILogger<OrderService>> _loggerOrderMock;

        protected BaseTest()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _loggerCustomerMock = new Mock<ILogger<CustomerService>>();
            _loggerProductMock = new Mock<ILogger<ProductService>>();
            _loggerCategoryMock = new Mock<ILogger<CategoryService>>();
            _loggerOrderMock = new Mock<ILogger<OrderService>>();


            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CustomerProfile>();
                cfg.AddProfile<ProductProfile>();
                cfg.AddProfile<CategoryProfile>();
                cfg.AddProfile<OrderProfile>();
            }, NullLoggerFactory.Instance);

            _mapper = config.CreateMapper();
        }
    }
}
