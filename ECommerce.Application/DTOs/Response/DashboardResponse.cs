using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.DTOs.Response
{
    public record DashboardResponse(
        DateOnly From,
        DateOnly To,
        SalesSummaryResponse Summary,
        List<DailySalesPoint> DailySales,
        List<TopProductItem> TopProducts,
        List<StatusBreakdownItem> StatusBreakdown);

    public record SalesSummaryResponse(
    int OrderCount,
    decimal GrossRevenue,
    decimal TaxCollected,
    decimal NetRevenue,
    decimal AverageOrderValue);

    public record DailySalesPoint(DateOnly Date, int OrderCount, decimal Revenue);

    public record TopProductItem(Guid ProductId, string Name, string Sku, int UnitsSold, decimal Revenue);

    public record StatusBreakdownItem(OrderStatus Status, string Label, int Count);
}