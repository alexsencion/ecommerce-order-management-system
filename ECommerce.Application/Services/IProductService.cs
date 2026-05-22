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
    public interface IProductService
    {
        Task<Result<PagedResponse<ProductResponse>>> GetAllAsync(ProductQueryParams query);
        Task<Result<ProductResponse>> GetByIdAsync(Guid id);
        Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request);
        Task<Result<ProductResponse>> UpdateAsync(Guid id, UpdateProductRequest request);
        Task<Result<ProductResponse>> AdjustStockAsync(Guid id, AdjustStockRequest request);
        Task<Result<bool>> DeactivateAsync(Guid id);
        Task<Result<bool>> DeleteAsync(Guid id);
    }
}
