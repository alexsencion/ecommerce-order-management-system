using AutoMapper;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(IUnitOfWork uow, IMapper mapper, ILogger<CustomerService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest request)
        {
            if (await _uow.Customers.EmailExistsAsync(request.Email))
                return Result<CustomerResponse>.Conflict($"Email '{request.Email}' is already registered.");

            var customer = _mapper.Map<Customer>(request);
            await _uow.Customers.AddAsync(customer);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Customer created: {CustomerId} ({Email})", customer.Id, customer.Email);

            return Result<CustomerResponse>.Created(_mapper.Map<CustomerResponse>(customer)); 
        }

        public async Task<Result<bool>> DeactivateAsync(Guid id)
        {
            var customer = await _uow.Customers.GetByIdAsync(id);
            if (customer == null)
                return Result<bool>.NotFound($"Customer {id} not found.");

            if (!customer.IsActive)
                return Result<bool>.Failure($"Customer is already inactive.");

            customer.IsActive = false;
            _uow.Customers.Update(customer);
            await _uow.SaveChangesAsync();

           _logger.LogInformation("Customer deactivated: {CustomerId}", id);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var customer = await _uow.Customers.GetByIdAsync(id);
            if (customer == null)
                return Result<bool>.NotFound($"Customer {id} not found.");

            _uow.Customers.Remove(customer);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Customer deleted: {CustomerId}", id);

            return Result<bool>.Success(true);
        }

        public async Task<Result<PagedResponse<CustomerResponse>>> GetAllAsync(CustomerQueryParams query)
        {
            var customers = await _uow.Customers.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                customers = customers.Where(c => 
                    c.FirstName.ToLower().Contains(search) || 
                    c.LastName.ToLower().Contains(search) ||
                    c.Email.ToLower().Contains(search));
            }

            if (query.IsActive.HasValue)
                customers = customers.Where(c => c.IsActive == query.IsActive.Value);

            var totalCount = customers.Count();
            var pageSize = query.ClampedPageSize;

            var paged = customers
                .Skip((query.Page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new PagedResponse<CustomerResponse>
            {
                Data = _mapper.Map<IEnumerable<CustomerResponse>>(paged),
                Page = query.Page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Result<PagedResponse<CustomerResponse>>.Success(response);
        }

        public async Task<Result<CustomerResponse>> GetByIdAsync(Guid id)
        {
            var customer = await _uow.Customers.GetByIdAsync(id);
            if (customer == null)
                return Result<CustomerResponse>.NotFound($"Customer {id} not found.");

            return Result<CustomerResponse>.Success(_mapper.Map<CustomerResponse>(customer));
        }

        public async Task<Result<CustomerResponse>> UpdateAsync(Guid id, UpdateCustomerRequest request)
        {
            var customer = await _uow.Customers.GetByIdAsync(id);
            if (customer == null)
                return Result<CustomerResponse>.NotFound($"Customer {id} not found.");

            _mapper.Map(request, customer);
            _uow.Customers.Update(customer);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Customer updated: {CustomerId}", id);

            return Result<CustomerResponse>.Success(_mapper.Map<CustomerResponse>(customer));
        }
    }
}
