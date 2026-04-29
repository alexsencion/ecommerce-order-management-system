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
    public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> EmailExistsAsync(string email) => await _dbSet.AnyAsync(c => c.Email == email.ToLower());

        public async Task<Customer?> GetByEmailAsync(string email) => await _dbSet.FirstOrDefaultAsync(c => c.Email == email.ToLower());
    }
}
