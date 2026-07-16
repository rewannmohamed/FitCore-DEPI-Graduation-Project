using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.AdminDashboard
{
    public class DashboardStatsDto
    {
        public int TotalMembers { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int ActivePlans { get; set; }
        public int DailyAttendance { get; set; }
    }
}
