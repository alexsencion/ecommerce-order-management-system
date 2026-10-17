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
    public class InvoiceRepository : GenericRepository<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(AppDbContext context) : base(context)
        {
        }

        public Task<int> CountIssuedInMonthAsync(int year, int month)
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            return _context.Invoices.CountAsync(i => i.IssuedAt >= start && i.IssuedAt < end);
        }

        public Task<Invoice?> GetByOrderIdAsync(Guid orderId) => 
            _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == orderId);
    }
}
