using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces.Repositories
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<Category?> GetBySlugAsync(string  slug);
        Task<bool> SlugExistsAsync(string slug);
        Task<bool> HasProductsAsync(Guid categoryId);
    }
}
