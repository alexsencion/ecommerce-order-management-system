using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Common
{
    public class FileStorageSettings
    {
        public const string SectionName = "FileStorage";
        public string RootPath { get; set; } = "storage";
    }
}
