using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces.Repositories;
using ECommerce.Domain.ReadModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public class ReportService : IReportService
    {
        public const int DefaultRangeDays = 30;
        public const int MaxRangeDays = 366;
        public const int MaxTopProducts = 50;

        private readonly IReportRepository _reports;
        public readonly TimeProvider _clock;

        public ReportService(IReportRepository reports, TimeProvider clock)
        {
            _reports = reports;
            _clock = clock;
        }

        public async Task<Result<DashboardResponse>> GetDashboardAsync(ReportQueryParams query)
        {
            var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
            var to = query.To ?? today;
            var from = query.From ?? to.AddDays(-(DefaultRangeDays - 1));

            if (from > to)
                return Result<DashboardResponse>.Failure("'from' must be on or before 'to'.");

            if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
                return Result<DashboardResponse>.Failure(
                    $"Date range cannot exceed {MaxRangeDays} days.");

            if (query.Top is < 1 or > MaxTopProducts)
                return Result<DashboardResponse>.Failure(
                    $"'top' must be between 1 and {MaxTopProducts}.");

            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var paid = OrderStatusGroups.Paid;

            var totals = await _reports.GetSalesTotalsAsync(fromUtc, toExclusiveUtc, paid);
            var daily = await _reports.GetDailySalesAsync(fromUtc, toExclusiveUtc, paid);
            var top = await _reports.GetTopProductsAsync(fromUtc, toExclusiveUtc, paid, query.Top);
            var counts = await _reports.GetStatusCountsAsync(fromUtc, toExclusiveUtc);

            return Result<DashboardResponse>.Success(new DashboardResponse(
                from,
                to,
                BuildSummary(totals),
                FillDailyGaps(from, to, daily),
                top.Select(p => new TopProductItem(
                    p.ProductId, p.Name, p.Sku, p.UnitsSold, p.Revenue)).ToList(),
                BuildStatusBreakdown(counts)));
        }

        private static SalesSummaryResponse BuildSummary(SalesTotals t) => new(
            t.OrderCount,
            t.GrossRevenue,
            t.TaxCollected,
            NetRevenue: t.GrossRevenue - t.TaxCollected,
            AverageOrderValue: t.OrderCount == 0
                ? 0m
                : Math.Round(t.GrossRevenue / t.OrderCount, 2));

        private static List<DailySalesPoint> FillDailyGaps(
            DateOnly from, DateOnly to, List<DailySales> rows)
        {
            var byDate = rows.ToDictionary(r => DateOnly.FromDateTime(r.Date));
            var points = new List<DailySalesPoint>();

            for (var day = from; day <= to; day = day.AddDays(1))
            {
                points.Add(byDate.TryGetValue(day, out var row)
                    ? new DailySalesPoint(day, row.OrderCount, row.Revenue)
                    : new DailySalesPoint(day, 0, 0m));
            }

            return points;
        }

        private static List<StatusBreakdownItem> BuildStatusBreakdown(List<StatusCount> counts)
        {
            var byStatus = counts.ToDictionary(c => c.Status, c => c.Count);

            return Enum.GetValues<OrderStatus>()
                .Select(s => new StatusBreakdownItem(
                    s, s.ToString(), byStatus.GetValueOrDefault(s)))
                .ToList();
        }
    }
}
