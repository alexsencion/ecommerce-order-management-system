using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.DTOs.Request
{
    public class OrderQueryParams
    {
        public Guid? CustomerId { get; set; }
        public OrderStatus? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int ClampedPageSize => Math.Min(Math.Max(PageSize, 1), 50); 
    }
}
