using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public interface ICustomerService
    {
        Task<Result<PagedResponse<CustomerResponse>>> GetAllAsync(CustomerQueryParams query);
        Task<Result<CustomerResponse>> GetByIdAsync(Guid id);
        Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest request);
        Task<Result<CustomerResponse>> UpdateAsync(Guid id, UpdateCustomerRequest request);
        Task<Result<bool>> DeactivateAsync(Guid id);
        Task<Result<bool>> DeleteAsync(Guid id);
    }
}
