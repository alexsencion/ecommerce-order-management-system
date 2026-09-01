using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.DTOs.Request
{
    public class CreatePaymentIntentRequest
    {
        public Guid OrderId { get; set; }
    }
}
