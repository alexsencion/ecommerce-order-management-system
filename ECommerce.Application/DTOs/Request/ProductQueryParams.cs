using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.DTOs.Request
{
    public class ProductQueryParams
    {
        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsActive { get; set; }
        public bool? InStockOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int ClampedPageSize => Math.Min(Math.Max(PageSize, 1), 50);
    }
}
