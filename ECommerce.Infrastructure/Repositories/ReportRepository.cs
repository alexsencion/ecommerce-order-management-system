using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces.Repositories;
using ECommerce.Domain.ReadModels;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly AppDbContext _db;
        public ReportRepository(AppDbContext db) => _db = db;

        private IQueryable<Order> OrdersIn(DateTime from, DateTime toExclusive) =>
            _db.Orders.AsNoTracking()
                .Where(o => o.CreatedAt >= from && o.CreatedAt <= toExclusive);

        public Task<List<DailySales>> GetDailySalesAsync(DateTime from, DateTime toExclusive, IReadOnlyCollection<OrderStatus> statuses)
        {
            var s = statuses.ToArray();
            return OrdersIn(from, toExclusive)
                .Where(o => s.Contains(o.Status))
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new DailySales(g.Key, g.Count(), g.Sum(o => o.Total)))
                .ToListAsync();
        }

        public async Task<SalesTotals> GetSalesTotalsAsync(DateTime from, DateTime toExclusive, IReadOnlyCollection<OrderStatus> statuses)
        {
            var s = statuses.ToArray();
            var q = OrdersIn(from, toExclusive).Where(o => s.Contains(o.Status));

            var count = await q.CountAsync();
            var gross = await q.SumAsync(o => (decimal?)o.Total) ?? 0m;
            var tax = await q.SumAsync(o => (decimal?)o.Tax) ?? 0m;

            return new SalesTotals(count, gross, tax);
        }

        public Task<List<StatusCount>> GetStatusCountsAsync(DateTime from, DateTime toExclusive) =>
            OrdersIn(from, toExclusive)
                .GroupBy(o => o.Status)
                .Select(g => new StatusCount(g.Key, g.Count()))
                .ToListAsync();

        public Task<List<ProductSales>> GetTopProductsAsync(DateTime from, DateTime toExclusive, IReadOnlyCollection<OrderStatus> statuses, int limit)
        {
            var s = statuses.ToArray();
            return _db.Set<OrderItem>().AsNoTracking()
                .Where(i => i.Order.CreatedAt >= from
                        && i.Order.CreatedAt < toExclusive
                        && s.Contains(i.Order.Status))
                .GroupBy(i => new { i.ProductId, i.Product.Name, i.Product.Sku })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.Name,
                    g.Key.Sku,
                    Units = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.Quantity * i.UnitPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(limit)
                .Select(x => new ProductSales(x.ProductId, x.Name, x.Sku, x.Units, x.Revenue))
                .ToListAsync();
        }
    }
}
