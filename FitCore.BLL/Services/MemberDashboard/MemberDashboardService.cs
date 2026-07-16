using FitCore.BLL.Interfaces.MemberDashboard;
using FitCore.DAL.Data.Contexts;
using FitCore.Shared.DTOs.MemberDashboard;
using FitCore.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using FitCore.DAL.Data.Models; 
namespace FitCore.BLL.Services
{
    public class MemberDashboardService : IMemberDashboardService
    {
        private readonly FitCoreDbContext _context;

        public MemberDashboardService(FitCoreDbContext context)
        {
            _context = context;
        }

        public async Task<ProfileStatsDto> GetProfileStatsAsync(int userId)
        {
            var member = await _context.MemberProfiles
                .FirstOrDefaultAsync(m => m.UserID == userId);

            if (member == null) return new ProfileStatsDto { MembershipStatus = "Not Found" };

            var activeMembership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.MemberProfileId == member.MemberProfileId && m.Status == MemberShipStatus.Active);

            var attendedCount = await _context.Attendances
                .CountAsync(a => a.UserId == userId);

            return new ProfileStatsDto
            {
                AttendancePercentage = attendedCount * 5.0,
                MembershipStatus = activeMembership != null ? "Active" : "Inactive"
            };
        }

        public async Task<NextClassDto> GetNextClassAsync(int userId)
        {
            var currentTime = DateTime.Now.TimeOfDay;

            return await _context.ClassSchedule
                .IgnoreQueryFilters()
                .Include(cs => cs.Class)
                .ThenInclude(c => c.Trainer)
                .ThenInclude(t => t.User)
                .Where(cs => cs.StartTime > currentTime)
                .OrderBy(cs => cs.StartTime)
                .Select(cs => new NextClassDto
                {
                    ClassName = cs.Class.ClassName,
                    StudioName = "Studio A",
                    TrainerName = cs.Class.Trainer.User.FullName,
                    StartTime = DateTime.Today.Add(cs.StartTime)
                })
                .FirstOrDefaultAsync() ?? new NextClassDto();
        }



        public async Task<List<NotificationDto>> GetNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new NotificationDto
                {
                    Title = n.Title,
                    Content = n.Content,
                    TimeAgo = n.CreatedAt.ToString("g")
                }).ToListAsync();
        }

        public async Task<DigitalPassDto> GetDigitalPassAsync(int userId)
        {
            var profile = await _context.MemberProfiles
                .Include(m => m.User)
                .Include(m => m.Memberships) 
                .ThenInclude(sub => sub.GymService)
                .FirstOrDefaultAsync(m => m.UserID == userId);

            var activeMembership = profile?.Memberships?.FirstOrDefault(m => m.Status == MemberShipStatus.Active);

            return new DigitalPassDto
            {
                MemberName = profile?.User.FullName ?? "N/A",
                MembershipType = activeMembership?.GymService?.Name ?? "No Plan",
                ValidUntil = activeMembership?.EndDate ?? DateTime.UtcNow,
                QrCodeData = profile?.QRCodeData ?? "N/A"
            };
        }
    }
}