using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.DTOs.Request
{
    public class ReportQueryParams
    {
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
        public int Top { get; set; } = 5;
    }
}
