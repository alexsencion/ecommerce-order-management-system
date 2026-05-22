using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.DTOs.Request
{
    public class AdjustStockRequest
    {
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
