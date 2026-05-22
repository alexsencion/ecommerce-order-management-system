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
        protected readonly Mock<IUnitOfWork> _ouwMock;
        protected readonly Mock<ILogger<CustomerService>> _loggerMock;

        protected BaseTest()
        {
            _ouwMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<CustomerService>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CustomerProfile>();
            }, NullLoggerFactory.Instance);

            _mapper = config.CreateMapper();
        }
    }
}
