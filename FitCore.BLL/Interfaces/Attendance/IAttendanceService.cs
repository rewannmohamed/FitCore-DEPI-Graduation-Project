using FitCore.Shared.DTOs.Attendance;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FitCore.BLL.Interfaces.Attendance
{
    public interface IAttendanceService
    {
        Task<MemberQrCodeDto> GetMyQrCodeAsync(int userId);
        Task<bool> GetMyStatusTodayAsync(int userId);
        Task<object> GetMyHistoryAsync(int userId, int page, int pageSize);
        Task<object> GetMyStatsAsync(int userId);

        public Task<CheckInResponseDto> CheckInByScanAsync(CheckInRequestDto request);

        Task<CheckInResponseDto> CheckInManualAsync(string searchInput);
        Task<object> SearchMembersAsync(string query); 
        Task<object> GetMemberCheckInSummaryAsync(int userId);
        Task<List<object>> GetRecentScansAsync();
        Task<object> GetDailyLogsAsync();

        Task<List<object>> GetAvailableClassesAsync();
        Task<object> GetClassAvailabilityAsync(int classId);
        Task<CheckInResponseDto> CheckInClassAsync(int userId, int classId);
    }
}