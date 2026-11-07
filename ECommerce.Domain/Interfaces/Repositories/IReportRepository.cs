using ECommerce.Domain.Enums;
using ECommerce.Domain.ReadModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces.Repositories
{
    public interface IReportRepository
    {
        Task<SalesTotals> GetSalesTotalsAsync(
            DateTime fromUtc, DateTime toUtcExclusive, IReadOnlyCollection<OrderStatus> statuses);

        Task<List<DailySales>> GetDailySalesAsync(
            DateTime fromUtc, DateTime toUtcExclusive, IReadOnlyCollection<OrderStatus> statuses);

        Task<List<ProductSales>> GetTopProductsAsync(
            DateTime fromUtc, DateTime toUtcExclusive, IReadOnlyCollection<OrderStatus> statuses, int limit);

        Task<List<StatusCount>> GetStatusCountsAsync(DateTime fromUtc, DateTime toUtcExclusive);
    }
}
