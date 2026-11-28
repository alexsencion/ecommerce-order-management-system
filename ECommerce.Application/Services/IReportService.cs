using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public interface IReportService
    {
        Task<Result<DashboardResponse>> GetDashboardAsync(ReportQueryParams query);
    }
}
