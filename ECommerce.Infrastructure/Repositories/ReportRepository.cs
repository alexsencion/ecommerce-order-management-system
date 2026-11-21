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

        public async Task<List<DailySales>> GetDailySalesAsync(DateTime from, DateTime toExclusive, IReadOnlyCollection<OrderStatus> statuses)
        {
            var s = statuses.ToArray();

            var rows = await OrdersIn(from, toExclusive)
                .Where(o => s.Contains(o.Status))
                .Select(o => new { o.CreatedAt, o.Total })
                .ToListAsync();

            return rows
                .GroupBy(r => r.CreatedAt.Date)
                .Select(g => new DailySales(g.Key, g.Count(), g.Sum(r => r.Total)))
                .OrderBy(d => d.Date)
                .ToList();
        }

        public async Task<SalesTotals> GetSalesTotalsAsync(DateTime from, DateTime toExclusive, IReadOnlyCollection<OrderStatus> statuses)
        {
            var s = statuses.ToArray();

            var rows = await OrdersIn(from, toExclusive)
                .Where(o => s.Contains(o.Status))
                .Select(o => new { o.Total, o.Tax })
                .ToListAsync();

            return new SalesTotals(
                rows.Count,
                rows.Sum(r => r.Total),
                rows.Sum(r => r.Tax));
        }

        public Task<List<StatusCount>> GetStatusCountsAsync(DateTime from, DateTime toExclusive) =>
            OrdersIn(from, toExclusive)
                .GroupBy(o => o.Status)
                .Select(g => new StatusCount(g.Key, g.Count()))
                .ToListAsync();

        public async Task<List<ProductSales>> GetTopProductsAsync(DateTime from, DateTime toExclusive, IReadOnlyCollection<OrderStatus> statuses, int limit)
        {
            var s = statuses.ToArray();

            var rows = await _db.Set<OrderItem>().AsNoTracking()
                .Where(i => i.Order.CreatedAt >= from
                         && i.Order.CreatedAt < toExclusive
                         && s.Contains(i.Order.Status))
                .Select(i => new
                {
                    i.ProductId,
                    i.Product.Name,
                    i.Product.Sku,
                    i.Quantity,
                    i.UnitPrice,
                })
                .ToListAsync();

            return rows
                .GroupBy(r => new { r.ProductId, r.Name, r.Sku })
                .Select(g => new ProductSales(
                    g.Key.ProductId, g.Key.Name, g.Key.Sku,
                    g.Sum(r => r.Quantity),
                    g.Sum(r => r.Quantity * r.UnitPrice)))
                .OrderByDescending(p => p.Revenue)
                .ThenBy(p => p.Name)
                .Take(limit)
                .ToList();
        }
    }
}
