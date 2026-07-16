using FitCore.Shared.DTOs.AdminDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.BLL.Interfaces.AdminDashboard
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync();
        Task<List<PlanDistributionDto>> GetPlanDistributionAsync();
        Task<List<RecentEnrolmentDto>> GetRecentEnrolmentsAsync();
        Task<List<RecentAlertDto>> GetRecentAlertsAsync();

        Task<IEnumerable<RevenueChartDto>> GetRevenueChartDataAsync(DateTime? startDate, DateTime? endDate);

        byte[] ExportRevenueToCsv(IEnumerable<RevenueChartDto> data);
    }
}
