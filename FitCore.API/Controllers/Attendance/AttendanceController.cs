using FitCore.BLL.Interfaces.Attendance;
using FitCore.BLL.Interfaces.Auth;
using FitCore.Shared.DTOs.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FitCore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ICurrentUserService _currentUserService;

        public AttendanceController(IAttendanceService attendanceService,ICurrentUserService currentUserService)
        {
            _attendanceService = attendanceService;
            _currentUserService = currentUserService;
        }


        [HttpGet("me/qrcode")]
        [Authorize]
        public async Task<IActionResult> GetMyQrCode()
        {
            var result = await _attendanceService.GetMyQrCodeAsync(_currentUserService.GetRequiredUserId());
            return Ok(result);
        }

       
        [HttpGet("me/status-today")]
        [Authorize]
        public async Task<IActionResult> GetMyStatusToday() => Ok(await _attendanceService.GetMyStatusTodayAsync(_currentUserService.GetRequiredUserId()));

        [HttpGet("me/history")]
        [Authorize]
        public async Task<IActionResult> GetMyHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10) => Ok(await _attendanceService.GetMyHistoryAsync(_currentUserService.GetRequiredUserId(), page, pageSize));

        [HttpGet("me/stats")]
        [Authorize]
        public async Task<IActionResult> GetMyStats() => Ok(await _attendanceService.GetMyStatsAsync(_currentUserService.GetRequiredUserId()));

        // ==========================================
        // 2) Reception / GymOps Terminal - 6 Endpoints
        // ==========================================


        [HttpPost("checkin/scan")]
        [Authorize(Roles = "Receptionist,Admin")]
        public async Task<IActionResult> CheckInByScan([FromBody] CheckInRequestDto request) => Ok(await _attendanceService.CheckInByScanAsync(request));

        [HttpPost("checkin/manual")]
        [Authorize(Roles = "Receptionist,Admin")]
        public async Task<IActionResult> CheckInManual([FromQuery] string searchInput) => Ok(await _attendanceService.CheckInManualAsync(searchInput));

        [HttpGet("members/search")]
        [Authorize(Roles = "Receptionist,Admin")]
        public async Task<IActionResult> SearchMembers([FromQuery] string query) => Ok(await _attendanceService.SearchMembersAsync(query));

        [HttpGet("members/{userId}/checkin-summary")]
        [Authorize(Roles = "Receptionist,Admin")]
        public async Task<IActionResult> GetMemberCheckInSummary([FromRoute] int userId) => Ok(await _attendanceService.GetMemberCheckInSummaryAsync(userId));

        [HttpGet("recent-scans")]
        [Authorize(Roles = "Receptionist,Admin")]
        public async Task<IActionResult> GetRecentScans() => Ok(await _attendanceService.GetRecentScansAsync());

        [HttpGet("daily-logs")]
        [Authorize(Roles = "Receptionist,Admin")]
        public async Task<IActionResult> GetDailyLogs() => Ok(await _attendanceService.GetDailyLogsAsync());


        // ==========================================
        // 3) Class Check-in  - 5 Endpoints
        // ==========================================
        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses() => Ok(await _attendanceService.GetAvailableClassesAsync());

        [HttpGet("classes/{id}/availability")]
        public async Task<IActionResult> GetClassAvailability([FromRoute] int id) => Ok(await _attendanceService.GetClassAvailabilityAsync(id));

        [HttpPost("checkin/class")]
        [Authorize]
        public async Task<IActionResult> CheckInClass([FromQuery] int classId) => Ok(await _attendanceService.CheckInClassAsync(_currentUserService.GetRequiredUserId(), classId));
    }
}
