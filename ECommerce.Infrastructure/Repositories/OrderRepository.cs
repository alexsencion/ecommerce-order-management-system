using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
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
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Order>> GetByCustomerAsync(Guid customerId) =>
            await _dbSet
                .Include(o => o.Items)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status) =>
            await _dbSet.Where(o => o.Status == status).ToListAsync();

        public async Task<Order?> GetWithDetailsAsync(Guid orderId) => 
            await _dbSet
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Customer)
                .Include(o => o.Payment)
                .Include(o => o.Shipment)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == orderId);
    }
}
