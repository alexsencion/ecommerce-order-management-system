using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Models
{
    public record InvoiceFile(byte[] Content, string FileName, string ContentType = "application/pdf");
}
