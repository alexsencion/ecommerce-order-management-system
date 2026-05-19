using ECommerce.Domain.Common;
using ECommerce.Domain.Interfaces.Repositories;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;
        private readonly Dictionary<Type, object> _repositories = new();

        public ICustomerRepository Customers { get; }
        public IProductRepository Products { get; }
        public IOrderRepository Orders { get; }
        public ICategoryRepository Categories { get; }

        public UnitOfWork(AppDbContext context,
                          ICustomerRepository customers,
                          IProductRepository products,
                          IOrderRepository orders,
                          ICategoryRepository categories) 
        {
            _context = context;
            Customers = customers;
            Products = products;
            Orders = orders;
            Categories = categories;
        }

        public async Task BeginTransactionAsync() => _transaction = await _context.Database.BeginTransactionAsync();

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.CommitAsync();
        }

        public IGenericRepository<T> Repository<T>() where T : BaseEntity
        {
            var type = typeof(T);
            if (!_repositories.ContainsKey(type))
                _repositories[type] = new GenericRepository<T>(_context);
            return (IGenericRepository<T>)_repositories[type];
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
