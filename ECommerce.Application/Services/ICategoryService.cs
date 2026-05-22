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
    public interface ICategoryService
    {
        Task<Result<IEnumerable<CategoryResponse>>> GetAllAsync();
        Task<Result<CategoryResponse>> GetByIdAsync(Guid id);
        Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request);
        Task<Result<CategoryResponse>> UpdateAsync(Guid id, UpdateCategoryRequest request);
        Task<Result<bool>> DeleteAsync(Guid id);
    }
}
