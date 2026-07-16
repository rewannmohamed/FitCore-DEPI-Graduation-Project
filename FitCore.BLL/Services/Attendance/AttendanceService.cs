using FitCore.BLL.Interfaces.Attendance;
using FitCore.DAL.Data.Contexts;
using FitCore.DAL.Data.Models;
using FitCore.Shared.DTOs.Attendance;
using FitCore.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AttendanceEntity = FitCore.DAL.Data.Models.Attendance;

namespace FitCore.BLL.Services.Attendance
{
    public class AttendanceService(FitCoreDbContext context) : IAttendanceService
    {
        #region 1) Self-Service (Member App)

        public async Task<MemberQrCodeDto> GetMyQrCodeAsync(int userId)
        {
            var profile = await context.Set<MemberProfile>()
                .FirstOrDefaultAsync(m => m.UserID == userId);

            if (profile == null)
                throw new KeyNotFoundException("Member profile not found.");

            if (string.IsNullOrWhiteSpace(profile.QRCodeData))
            {
                profile.QRCodeData = GenerateQrCode();
                await context.SaveChangesAsync();
            }

            return new MemberQrCodeDto { QrCode = profile.QRCodeData };
        }


        public async Task<bool> GetMyStatusTodayAsync(int userId)
        {
            var today = DateTime.UtcNow.Date;
            return await context.Set<AttendanceEntity>()
                .AnyAsync(a => a.MemberProfile.UserID == userId && a.CheckInTime.Date == today);
        }

        public async Task<object> GetMyHistoryAsync(int userId, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 50) pageSize = 10;

            var query = context.Set<AttendanceEntity>()
                .Where(a => a.MemberProfile.UserID == userId)
                .OrderByDescending(a => a.CheckInTime);

            int totalRecords = await query.CountAsync();

            var history = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.AttendanceID,
                    Date = a.CheckInTime.ToString("yyyy-MM-dd"),
                    Time = a.CheckInTime.ToString("hh:mm tt"),
                    Type = a.Type.ToString()
                })
                .ToListAsync();

            return new
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                History = history
            };
        }

        public async Task<object> GetMyStatsAsync(int userId)
        {
            var profile = await context.Set<MemberProfile>()
                .Include(m => m.Memberships)
                .FirstOrDefaultAsync(m => m.UserID == userId);

            if (profile == null)
                throw new KeyNotFoundException("Member profile not found.");

            var activeMembership = profile.Memberships
                .FirstOrDefault(m => m.Status == MemberShipStatus.Active);

            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            int totalDaysThisMonth = await context.Set<AttendanceEntity>()
                .Where(a => a.MemberProfile.UserID == userId && a.CheckInTime >= monthStart)
                .Select(a => a.CheckInTime.Date)
                .Distinct()
                .CountAsync();

            int allowedDays = activeMembership?.GymService?.AllowedSessionsCount ?? DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month);
            double rate = allowedDays > 0 ? Math.Min(100.0, (totalDaysThisMonth * 100.0) / allowedDays) : 0;

            return new
            {
                AttendanceRate = $"{rate:0}%",
                TotalDays = totalDaysThisMonth,
                AllowedDays = allowedDays
            };
        }

        #endregion

        #region 2) Reception / GymOps Terminal

        public async Task<CheckInResponseDto> CheckInByScanAsync(CheckInRequestDto request)
        {
            var profile = await context.Set<MemberProfile>()
                .Include(m => m.User)
                .Include(m => m.Memberships).ThenInclude(mb => mb.GymService)
                .FirstOrDefaultAsync(m => m.QRCodeData == request.QrCode);

            if (profile == null)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Invalid QR code." };
            }

            return await RecordCheckIn(profile, request.ClassScheduleId.HasValue ? AttendenceType.ClassSession : AttendenceType.OpenGym,
                request.ClassScheduleId.HasValue ? "Class check-in successful." : "Check-in successful.");
        }

        public async Task<CheckInResponseDto> CheckInManualAsync(string searchInput)
        {
            if (string.IsNullOrWhiteSpace(searchInput))
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Please provide a member ID, email, or phone number." };
            }

            var profile = await FindMemberProfileAsync(searchInput);

            if (profile == null)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = $"No member found matching '{searchInput}'." };
            }

            return await RecordCheckIn(profile, AttendenceType.OpenGym, $"Manual check-in successful for: {searchInput}");
        }

        public async Task<object> SearchMembersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<object>();

            var lowered = query.ToLower();

            var members = await context.Set<MemberProfile>()
                .Include(m => m.User)
                .Where(m => m.User.FullName.ToLower().Contains(lowered)
                    || m.User.Email.ToLower().Contains(lowered)
                    || m.User.PhoneNumber.Contains(query))
                .Take(20)
                .Select(m => new
                {
                    UserId = m.UserID,
                    Name = m.User.FullName,
                    Phone = m.User.PhoneNumber,
                    Email = m.User.Email
                })
                .ToListAsync();

            return members;
        }

        public async Task<object> GetMemberCheckInSummaryAsync(int userId)
        {
            var profile = await context.Set<MemberProfile>()
                .Include(m => m.User)
                .Include(m => m.Memberships).ThenInclude(mb => mb.GymService)
                .Include(m => m.Attendances)
                .FirstOrDefaultAsync(m => m.UserID == userId);

            if (profile == null)
                throw new KeyNotFoundException("Member not found.");

            var activeMembership = profile.Memberships.FirstOrDefault(m => m.Status == MemberShipStatus.Active);
            var lastCheckIn = profile.Attendances.OrderByDescending(a => a.CheckInTime).FirstOrDefault();

            var warnings = new List<string>();
            if (activeMembership == null)
            {
                warnings.Add("No active membership.");
            }
            else if (activeMembership.EndDate <= DateTime.UtcNow.AddDays(7))
            {
                warnings.Add("Membership expiring soon.");
            }

            return new
            {
                UserId = profile.UserID,
                FullName = profile.User.FullName,
                Status = activeMembership?.Status.ToString() ?? "No Membership",
                Package = activeMembership?.GymService?.Name ?? "N/A",
                Warnings = warnings,
                LastCheckIn = lastCheckIn != null ? lastCheckIn.CheckInTime.ToString("g") : "Never"
            };
        }

        public async Task<List<object>> GetRecentScansAsync()
        {
            var scans = await context.Set<AttendanceEntity>()
                .Include(a => a.MemberProfile).ThenInclude(m => m.User)
                .OrderByDescending(a => a.CheckInTime)
                .Take(15)
                .Select(a => new
                {
                    a.AttendanceID,
                    a.UserId,
                    FullName = a.MemberProfile.User.FullName,
                    a.CheckInTime,
                    Type = a.Type.ToString()
                })
                .ToListAsync();

            return scans.Cast<object>().ToList();
        }

        public async Task<object> GetDailyLogsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var logs = await context.Set<AttendanceEntity>()
                .Include(a => a.MemberProfile).ThenInclude(m => m.User)
                .Where(a => a.CheckInTime.Date == today)
                .OrderByDescending(a => a.CheckInTime)
                .Select(a => new
                {
                    a.AttendanceID,
                    a.UserId,
                    FullName = a.MemberProfile.User.FullName,
                    a.CheckInTime,
                    Type = a.Type.ToString()
                })
                .ToListAsync();

            return new
            {
                TotalLogsToday = logs.Count,
                Logs = logs
            };
        }

        #endregion

        #region 3) Class Check-in

        public async Task<List<object>> GetAvailableClassesAsync()
        {
            var today = DateTime.UtcNow;
            var classes = await context.Set<Class>()
                .Include(c => c.Trainer).ThenInclude(t => t.User)
                .Include(c => c.Schedules)
                .Where(c => c.Status == ClassStatus.Active)
                .ToListAsync();

            var result = new List<object>();
            foreach (var c in classes)
            {
                var nextSchedule = c.Schedules
                    .OrderBy(s => s.StartTime)
                    .FirstOrDefault();

                int bookedCount = await context.Set<Booking>()
                    .CountAsync(b => b.ClassID == c.ClassID && b.Status != BookingStatus.Cancelled);

                result.Add(new
                {
                    ClassId = c.ClassID,
                    Name = c.ClassName,
                    Trainer = c.Trainer?.User?.FullName ?? "N/A",
                    Time = nextSchedule != null ? nextSchedule.StartTime.ToString(@"hh\:mm") : "N/A",
                    Capacity = $"{bookedCount}/{c.Capacity}"
                });
            }

            return result;
        }

        public async Task<object> GetClassAvailabilityAsync(int classId)
        {
            var gymClass = await context.Set<Class>()
                .Include(c => c.Schedules)
                .FirstOrDefaultAsync(c => c.ClassID == classId);

            if (gymClass == null)
                throw new KeyNotFoundException("Class not found.");

            int bookedCount = await context.Set<Booking>()
                .CountAsync(b => b.ClassID == classId && b.Status != BookingStatus.Cancelled);

            int remaining = Math.Max(0, gymClass.Capacity - bookedCount);
            double fillRate = gymClass.Capacity > 0 ? (bookedCount * 100.0) / gymClass.Capacity : 0;

            return new
            {
                ClassId = classId,
                FillRate = $"{fillRate:0}% Full",
                RemainingSlots = remaining
            };
        }

        public async Task<CheckInResponseDto> CheckInClassAsync(int userId, int classId)
        {

            var profile = await context.Set<MemberProfile>()
                .Include(m => m.User)
                .Include(m => m.Memberships).ThenInclude(mb => mb.GymService)
                .FirstOrDefaultAsync(m => m.UserID == userId);

            if (profile == null)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Member not found." };
            }

            var gymClass = await context.Set<Class>().FirstOrDefaultAsync(c => c.ClassID == classId);
            if (gymClass == null)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Class not found." };
            }
            

            return await RecordCheckIn(profile, AttendenceType.ClassSession, $"Checked in to {gymClass.ClassName} successfully.");
        }

        #endregion

        #region Helpers

        private async Task<MemberProfile?> FindMemberProfileAsync(string searchInput)
        {
            var lowered = searchInput.ToLower();

            if (int.TryParse(searchInput, out int userId))
            {
                var byId = await context.Set<MemberProfile>()
                    .Include(m => m.User)
                    .Include(m => m.Memberships).ThenInclude(mb => mb.GymService)
                    .FirstOrDefaultAsync(m => m.UserID == userId);
                if (byId != null) return byId;
            }

            return await context.Set<MemberProfile>()
                .Include(m => m.User)
                .Include(m => m.Memberships).ThenInclude(mb => mb.GymService)
                .FirstOrDefaultAsync(m => m.User.Email.ToLower() == lowered
                    || m.User.PhoneNumber == searchInput
                    || m.User.FullName.ToLower().Contains(lowered));
        }

        private async Task<CheckInResponseDto> RecordCheckIn(MemberProfile profile, AttendenceType type, string successMessage)
        {
            var activeMembership = profile.Memberships.FirstOrDefault(m => m.Status == MemberShipStatus.Active);

            var attendance = new AttendanceEntity
            {

                UserId = profile.MemberProfileId,
                MembershipID = activeMembership?.MembershipID,
                Type = type,
                CheckInTime = DateTime.UtcNow
            };

            context.Set<AttendanceEntity>().Add(attendance);

            if (activeMembership != null)
            {
                if (activeMembership.RemainingSessions.HasValue)
                {
                    activeMembership.RemainingSessions = Math.Max(0, activeMembership.RemainingSessions.Value - 1);
                }
            }

            await context.SaveChangesAsync();

            return new CheckInResponseDto
            {
                IsSuccess = true,
                Message = successMessage,
                MemberName = profile.User.FullName,
                MembershipType = activeMembership?.GymService?.Name ?? "No Active Plan",
                RemainingSessions = activeMembership?.RemainingSessions ?? 0,
                ExpiryDate = activeMembership?.EndDate ?? DateTime.UtcNow,
                CheckInTime = attendance.CheckInTime
            };
        }

        private static string GenerateQrCode() => $"FIT-{Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper()}";

        #endregion
    }
}