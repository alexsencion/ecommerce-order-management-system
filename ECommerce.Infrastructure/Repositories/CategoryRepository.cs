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
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Category?> GetBySlugAsync(string slug) => 
            await _dbSet.FirstOrDefaultAsync(c => c.Slug == slug.ToLower());


        public async Task<bool> HasProductsAsync(Guid categoryId) =>
            await _context.Set<Product>().AnyAsync(p => p.CategoryId == categoryId);

        public async Task<bool> SlugExistsAsync(string slug) =>
            await _dbSet.AnyAsync(c => c.Slug == slug.ToLower());
    }
}
