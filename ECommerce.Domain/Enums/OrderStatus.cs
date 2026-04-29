using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Enums
{
    public enum OrderStatus
    {
        Pending = 1,
        Confirmed = 2,
        Processing = 3,
        Packed = 4,
        Shipped = 5,
        Delivered = 6, 
        Cancelled = 7
    }
}
