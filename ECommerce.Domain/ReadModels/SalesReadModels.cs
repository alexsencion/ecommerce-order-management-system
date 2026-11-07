using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.ReadModels
{
    public record SalesTotals(int OrderCount, decimal GrossRevenue, decimal TaxCollected);
    public record DailySales(DateTime Date, int OrderCount, decimal Revenue);
    public record ProductSales(Guid ProductId, string Name, string Sku, int UnitsSold, decimal Revenue);
    public record StatusCount(OrderStatus Status, int Count);
}
