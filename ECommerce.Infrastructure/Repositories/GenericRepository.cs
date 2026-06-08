using ECommerce.Domain.Common;
using ECommerce.Domain.Interfaces.Repositories;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }
        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => await _dbSet.Where(predicate).ToListAsync();

        public virtual async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

        public virtual async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

        public void Remove(T entity) =>  _dbSet.Remove(entity);

        public void Update(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }
    }
}
