using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces.Repositories
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetWithDetailsAsync(Guid orderId);
        Task<IEnumerable<Order>> GetByCustomerAsync(Guid customerId);
        Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
    }
}
