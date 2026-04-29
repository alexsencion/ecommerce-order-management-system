using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces.Repositories
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId);
        Task<Product?> GetBySkuAsync(string sku);
        Task<bool> HasSufficientStockAsync(Guid productId, int quantity);
    }
}
