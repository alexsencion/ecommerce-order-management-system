using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Enums
{
    public enum PaymentStatus
    {
        Pending = 1,
        Succeeded = 2,
        Failed = 3,
        Refunded = 4
    }
}
