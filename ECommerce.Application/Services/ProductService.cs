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
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IUnitOfWork uow, IMapper mapper, ILogger<ProductService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<Result<ProductResponse>> AdjustStockAsync(Guid id, AdjustStockRequest request)
        {
            var product = await _uow.Products.GetByIdAsync(id);
            if (product == null)
                return Result<ProductResponse>.NotFound($"Product {id} not found.");

            var newStock = product.StockQuantity + request.Quantity;
            if (newStock < 0)
                return Result<ProductResponse>.Failure(
                    $"Adjustment would result in negative stock." +
                    $"Current stock: {product.StockQuantity}, adjustment: {request.Quantity}.");

            product.StockQuantity = newStock;
            _uow.Products.Update(product);
            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "Stock adjusted for product {ProductId}: {Delta} ({Reason}). New stock: {Stock}",
                id, request.Quantity, request.Reason, newStock);

            return Result<ProductResponse>.Success(_mapper.Map<ProductResponse>(product));
        }

        public async Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request)
        {
            var categoryExists = await _uow.Categories.GetByIdAsync(request.CategoryId);
            if (categoryExists == null)
                return Result<ProductResponse>.Failure(
                    $"Category {request.CategoryId} does not exist.", 422);

            var skuExists = await _uow.Products.GetBySkuAsync(request.Sku);
            if (skuExists != null)
                return Result<ProductResponse>.Conflict(
                    $"SKU '{request.Sku}' is already in use.");

            var product = _mapper.Map<Product>(request);
            await _uow.Products.AddAsync(product);
            await _uow.SaveChangesAsync();

            var created = await _uow.Products.GetByIdAsync(product.Id);
            if (created == null)
            {
                _logger.LogError(
                    "Product {ProductId} was created but could not be reloaded.",
                    product.Id);

                return Result<ProductResponse>.Failure(
                    "Error retrieving created product.",
                    500);
            }

            _logger.LogInformation("Product created: {ProductId} (SKU: {Sku})",
                product.Id, product.Sku);

            return Result<ProductResponse>.Created(_mapper.Map<ProductResponse>(created));
        }

        public async Task<Result<bool>> DeactivateAsync(Guid id)
        {
            var product = await _uow.Products.GetByIdAsync(id);
            if (product == null)
                return Result<bool>.NotFound($"Product {id} not found.");

            if (!product.IsActive)
                return Result<bool>.Failure($"Product is already inactive.");

            product.IsActive = false;
            _uow.Products.Update(product);
            await _uow.SaveChangesAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var product = await _uow.Products.GetByIdAsync(id);
            if (product == null)
                return Result<bool>.NotFound($"Product {id} not found.");

            if (product.ReservedQuantity > 0)
                return Result<bool>.Failure(
                    "Cannot delete a product with active stock reservations.");

            _uow.Products.Remove(product);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Product deleted: {ProductId}", id);
            return Result<bool>.Success(true);
        }

        public async Task<Result<PagedResponse<ProductResponse>>> GetAllAsync(ProductQueryParams query)
        {
            var products = await _uow.Products.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                products = products.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    p.Sku.ToLower().Contains(search) ||
                    (p.Description != null && p.Description.ToLower().Contains(search)));
            }

            if (query.CategoryId.HasValue)
                products = products.Where(p => p.CategoryId == query.CategoryId.Value);

            if (query.MinPrice.HasValue)
                products = products.Where(p => p.Price == query.MinPrice.Value);

            if (query.MaxPrice.HasValue)
                products = products.Where(p => p.Price == query.MaxPrice.Value);

            if (query.IsActive.HasValue)
                products = products.Where(p => p.IsActive == query.IsActive.Value);

            if (query.InStockOnly == true)
                products = products.Where(p => p.AvailableStock > 0);

            var totalCount = products.Count();
            var pageSize = query.ClampedPageSize;

            var paged = products
                .Skip((query.Page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new PagedResponse<ProductResponse>
            {
                Data = _mapper.Map<IEnumerable<ProductResponse>>(paged),
                Page = query.Page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Result<PagedResponse<ProductResponse>>.Success(response);
        }

        public async Task<Result<ProductResponse>> GetByIdAsync(Guid id)
        {
            var product = await _uow.Products.GetByIdAsync(id);
            if (product == null)
                return Result<ProductResponse>.NotFound($"Product {id} not found.");

            return Result<ProductResponse>.Success(_mapper.Map<ProductResponse>(product));
        }

        public async Task<Result<ProductResponse>> UpdateAsync(Guid id, UpdateProductRequest request)
        {
            var product = await _uow.Products.GetByIdAsync(id);
            if (product == null)
                return Result<ProductResponse>.NotFound($"Product {id} not found.");

            var categoryExists = await _uow.Categories.GetByIdAsync(request.CategoryId);
            if (categoryExists == null)
                return Result<ProductResponse>.Failure(
                    $"Category {request.CategoryId} does not exist.", 422);

            _mapper.Map(request, product);
            _uow.Products.Update(product);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Product updated: {ProductId}", id);
            return Result<ProductResponse>.Success(_mapper.Map<ProductResponse>(product));
        }
    }
}
