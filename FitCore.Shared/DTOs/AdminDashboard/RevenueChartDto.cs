using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.AdminDashboard
{
    public class RevenueChartDto
    {
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public int Year { get; set; } 
        public int Month { get; set; }
    }
}
