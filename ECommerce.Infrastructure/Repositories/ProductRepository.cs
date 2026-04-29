using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces.Repositories;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId) => await _dbSet.Where(p => p.CategoryId == categoryId && p.IsActive).ToListAsync();

        public async Task<Product?> GetBySkuAsync(string sku) => await _dbSet.FirstOrDefaultAsync(p => p.Sku == sku);

        public async Task<bool> HasSufficientStockAsync(Guid productId, int quantity)
        {
            var product = await _dbSet.FindAsync(productId);
            return product != null && product.AvailableStock >= quantity;
        }
    }
}
