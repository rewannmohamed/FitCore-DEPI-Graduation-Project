using FitCore.BLL.Interfaces;
using FitCore.BLL.Interfaces.AdminDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/admin/[controller]")]
[ApiController]
[Authorize]
public class AdminDashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public AdminDashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats() => Ok(await _dashboardService.GetStatsAsync());

    [HttpGet("plan-distribution")]
    public async Task<IActionResult> GetPlanDistribution() => Ok(await _dashboardService.GetPlanDistributionAsync());

    [HttpGet("alerts/recent")]
    public async Task<IActionResult> GetRecentAlerts() => Ok(await _dashboardService.GetRecentAlertsAsync());

    [HttpGet("members/recent")]
    public async Task<IActionResult> GetRecentEnrolments()
     => Ok(await _dashboardService.GetRecentEnrolmentsAsync());

    [HttpGet("revenue-chart")]
    public async Task<IActionResult> GetRevenueChart(DateTime? startDate, DateTime? endDate)
    {

        var data = await _dashboardService.GetRevenueChartDataAsync(startDate, endDate);
        return Ok(data);
    }

    [AllowAnonymous]
    [HttpGet("export-report")]
    public async Task<IActionResult> ExportReport(DateTime? startDate, DateTime? endDate)
    {
        var data = await _dashboardService.GetRevenueChartDataAsync(startDate, endDate);

        var fileContent = _dashboardService.ExportRevenueToCsv(data);

        string fileName = $"RevenueReport_{DateTime.Now:yyyyMMdd}.csv";
        return File(fileContent, "text/csv", fileName);
    }
}