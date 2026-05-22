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
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(IUnitOfWork uow, IMapper mapper, ILogger<CategoryService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request)
        {
            if (await _uow.Categories.SlugExistsAsync(request.Slug))
                return Result<CategoryResponse>.Conflict(
                    $"Slug '{request.Slug}' is already in use.");

            var category = _mapper.Map<Category>(request);
            await _uow.Categories.AddAsync(category);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Category created: {CategoryId} ({Slug})", 
                category.Id, category.Slug);

            return Result<CategoryResponse>.Created(_mapper.Map<CategoryResponse>(category));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var category = await _uow.Categories.GetByIdAsync(id);
            if (category == null)
                return Result<bool>.NotFound($"Category {id} not found.");

            if (await _uow.Categories.HasProductsAsync(id))
                return Result<bool>.Failure(
                    "Cannot delete a category that has products assigned to it.", 409);

            _uow.Categories.Remove(category);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Category deleted: {CategoryId}", id);
            return Result<bool>.Success(true);
        }

        public async Task<Result<IEnumerable<CategoryResponse>>> GetAllAsync()
        {
            var categories = await _uow.Categories.GetAllAsync();
            return Result<IEnumerable<CategoryResponse>>.Success(
                _mapper.Map<IEnumerable<CategoryResponse>>(categories));
        }

        public async Task<Result<CategoryResponse>> GetByIdAsync(Guid id)
        {
            var category = await _uow.Categories.GetByIdAsync(id);
            if (category == null)
                return Result<CategoryResponse>.NotFound($"Category {id} not found.");

            return Result<CategoryResponse>.Success(_mapper.Map<CategoryResponse>(category));
        }

        public async Task<Result<CategoryResponse>> UpdateAsync(Guid id, UpdateCategoryRequest request)
        {
            var category = await _uow.Categories.GetByIdAsync(id);
            if (category == null)
                return Result<CategoryResponse>.NotFound($"Category {id} not found.");

            var slugChanged = !string.Equals(category.Slug, 
                request.Slug.ToLower().Trim(), StringComparison.Ordinal);

            if (slugChanged && await _uow.Categories.SlugExistsAsync(request.Slug))
                return Result<CategoryResponse>.Conflict(
                    $"Slug '{request.Slug}' is already in use.");

            _mapper.Map(request, category);
            _uow.Categories.Update(category);
            await _uow.SaveChangesAsync();

            return Result<CategoryResponse>.Success(_mapper.Map<CategoryResponse>(category));
        }
    }
}
