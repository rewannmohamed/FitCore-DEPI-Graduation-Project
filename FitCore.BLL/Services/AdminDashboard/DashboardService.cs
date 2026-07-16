using FitCore.BLL.Interfaces;
using FitCore.BLL.Interfaces.AdminDashboard;
using FitCore.DAL.Data.Contexts;
using FitCore.Shared.DTOs.AdminDashboard;
using FitCore.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FitCore.BLL.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly FitCoreDbContext _context;

        public DashboardService(FitCoreDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var currentMonth = DateTime.UtcNow.Month;

            return new DashboardStatsDto
            {
                TotalMembers = await _context.MemberProfiles.CountAsync(),
                MonthlyRevenue = await _context.Payments
                    .Where(p => p.PaymentDate.Month == currentMonth)
                    .SumAsync(p => p.AmountPaid),
                ActivePlans = await _context.Memberships
                    .CountAsync(m => m.Status == MemberShipStatus.Active),
                DailyAttendance = await _context.Attendances
                    .CountAsync(a => a.CheckInTime.Date == today)
            };
        }

        public async Task<List<PlanDistributionDto>> GetPlanDistributionAsync()
        {
            var totalMemberships = await _context.Memberships.CountAsync();

            return await _context.Memberships
                .GroupBy(m => m.GymService.Name)
                .Select(g => new PlanDistributionDto
                {
                    PlanName = g.Key,
                    Count = g.Count(),
                    Percentage = totalMemberships > 0 ? (decimal)g.Count() / totalMemberships * 100 : 0
                }).ToListAsync();
        }

        public async Task<List<RecentEnrolmentDto>> GetRecentEnrolmentsAsync()
        {
            return await _context.Memberships
                .Include(m => m.MemberProfile)
                .ThenInclude(mp => mp.User)
                .Include(m => m.GymService)
                .OrderByDescending(m => m.StartDate)
                .Take(5)
                .Select(m => new RecentEnrolmentDto
                {
                    MemberName = m.MemberProfile.User.FullName,
                    PlanType = m.GymService.Name,
                    Status = m.Status.ToString(),
                    JoinDate = m.StartDate
                }).ToListAsync();
        }

        public async Task<List<RecentAlertDto>> GetRecentAlertsAsync()
        {
            return await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new RecentAlertDto
                {
                    Title = n.Title,
                    Content = n.Content,
                    Type = n.Type.ToString(),
                    TimeAgo = n.CreatedAt.ToString("g")
                }).ToListAsync();
        }

        public async Task<IEnumerable<RevenueChartDto>> GetRevenueChartDataAsync(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Now.AddDays(-30);
            var end = endDate ?? DateTime.Now;

            var revenueData = await _context.InvoiceItems
                .Where(i => i.Invoice.IssueDate >= start && i.Invoice.IssueDate <= end)
                .GroupBy(i => new { i.Invoice.IssueDate.Year, i.Invoice.IssueDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.LineTotal) })
                .ToListAsync();

            var expenseData = await _context.InventoryTransactionsItems
                .Where(i => i.Transaction.TransactionDate >= start && i.Transaction.TransactionDate <= end)
                .GroupBy(i => new { i.Transaction.TransactionDate.Year, i.Transaction.TransactionDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Quantity * x.UnitCost) })
                .ToListAsync();

            var allMonths = revenueData.Select(r => new { r.Year, r.Month })
                .Union(expenseData.Select(e => new { e.Year, e.Month }))
                .Distinct();

            return allMonths.Select(m => new RevenueChartDto
            {
                Year = m.Year,
                Month = m.Month,
                MonthName = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m.Month)} {m.Year}",
                TotalRevenue = revenueData.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)?.Total ?? 0,
                TotalExpenses = expenseData.FirstOrDefault(e => e.Year == m.Year && e.Month == m.Month)?.Total ?? 0
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();
        }

        public byte[] ExportRevenueToCsv(IEnumerable<RevenueChartDto> data)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Month,Total Revenue,Total Expenses");

            foreach (var item in data)
            {
                builder.AppendLine($"{item.MonthName},{item.TotalRevenue},{item.TotalExpenses}");
            }

            return Encoding.UTF8.GetBytes(builder.ToString());
        }
    }
}