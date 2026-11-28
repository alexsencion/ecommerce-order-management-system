using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Common
{
    public class InvoiceSettings
    {
        public const string SectionName = "Invoice";
        public string CompanyName { get; set; } = "ECommerce OMS";
        public string CompanyAddress { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;
        public string TaxRateLabel { get; set; } = "18%";
    }
}
