using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces.PrivateSessions;
using FitCore.DAL.Data.Contexts;
using FitCore.DAL.Data.Models;
using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.PrivateSessions;
using FitCore.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitCore.BLL.Services.PrivateSessions
{
    public class PrivateSessionService(FitCoreDbContext DbContext) : IPrivateSessionService
    {
        public async Task<PrivateSessionDto> CreatePrivateSessionAsync(CreatePrivateSessionDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.EndTime <= dto.StartTime) throw new ValidationException("End time must be after start time.");
            if (dto.SessionDate.Date < DateTime.UtcNow.Date) throw new BusinessRuleException("Cannot schedule in the past.");

            var trainer = await DbContext.Set<Trainer>().Include(t => t.User).FirstOrDefaultAsync(t => t.TrainerID == dto.TrainerID);
            if (trainer == null) throw new KeyNotFoundException("No trainer found with this id.");

            var member = await DbContext.Set<MemberProfile>().Include(m => m.User).FirstOrDefaultAsync(m => m.UserID == dto.MemberUserId);
            if (member == null) throw new KeyNotFoundException("No member profile found for this user.");

           
            await EnsureParticipantsAvailableAsync(dto.TrainerID, member.MemberProfileId, dto.SessionDate.Date, dto.StartTime, dto.EndTime, null);

            var session = new PrivateSession
            {
                TrainerID = dto.TrainerID,
                MemberUserId = member.MemberProfileId, 
                SessionDate = dto.SessionDate.Date,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = PrivateSessionStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            };

            await DbContext.Set<PrivateSession>().AddAsync(session);
            await DbContext.SaveChangesAsync();

            return MapToDto(session, trainer.User.FullName, member.User.FullName);
        }

        public async Task<PrivateSessionDto> AssignTrainerAsync(int privateSessionId, int trainerId)
        {
            var session = await DbContext.Set<PrivateSession>()
                .Include(s => s.MemberProfile).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(s => s.PrivateSessionID == privateSessionId);

            if (session == null)
            {
                throw new KeyNotFoundException("No private session found with this id.");
            }

            var trainer = await DbContext.Set<Trainer>().Include(t => t.User).FirstOrDefaultAsync(t => t.TrainerID == trainerId);
            if (trainer == null)
            {
                throw new KeyNotFoundException("No trainer found with this id.");
            }

            if (session.Status != PrivateSessionStatus.Scheduled)
            {
                throw new BusinessRuleException("Only scheduled sessions can be reassigned.");
            }

            await EnsureTrainerAvailableAsync(trainerId, session.SessionDate, session.StartTime, session.EndTime, session.PrivateSessionID);

            session.TrainerID = trainerId;
            DbContext.Set<PrivateSession>().Update(session);
            await DbContext.SaveChangesAsync();

            return MapToDto(session, trainer.User.FullName, session.MemberProfile.User.FullName);
        }

        public async Task<ICollection<PrivateSessionDto>> GetSessionsByTrainerAsync(int trainerId)
        {
            var trainer = await DbContext.Set<Trainer>()
               .Include(t => t.WorkingHoursSchedule)
               .FirstOrDefaultAsync(t => t.UserID == trainerId);

            if (trainer == null)
            {
                throw new KeyNotFoundException("No trainer found with this id.");
            }

            var sessions = await DbContext.Set<PrivateSession>()
                .Include(s => s.Trainer).ThenInclude(t => t.User)
                .Include(s => s.MemberProfile).ThenInclude(m => m.User)
                .Where(s => s.TrainerID == trainer.TrainerID)
                .OrderByDescending(s => s.SessionDate)
                .ToListAsync();

            return sessions.Select(s => MapToDto(s, s.Trainer.User.FullName, s.MemberProfile.User.FullName)).ToList();
        }

        public async Task<ICollection<PrivateSessionDto>> GetSessionsByMemberAsync(int memberUserId)
        {
            var sessions = await DbContext.Set<PrivateSession>()
                .Include(s => s.Trainer).ThenInclude(t => t.User)
                .Include(s => s.MemberProfile).ThenInclude(m => m.User)
                .Where(s => s.MemberUserId == memberUserId)
                .OrderByDescending(s => s.SessionDate)
                .ToListAsync();

            return sessions.Select(s => MapToDto(s, s.Trainer.User.FullName, s.MemberProfile.User.FullName)).ToList();
        }

        public async Task<bool> CancelSessionAsync(int privateSessionId)
        {
            var session = await DbContext.Set<PrivateSession>().FirstOrDefaultAsync(s => s.PrivateSessionID == privateSessionId);
            if (session == null)
            {
                throw new KeyNotFoundException("No private session found with this id.");
            }

            if (session.Status != PrivateSessionStatus.Scheduled)
            {
                throw new BusinessRuleException("Only scheduled sessions can be cancelled.");
            }

            session.Status = PrivateSessionStatus.Cancelled;
            DbContext.Set<PrivateSession>().Update(session);
            var affected = await DbContext.SaveChangesAsync();

            return affected > 0;
        }

        public async Task<bool> CompleteSessionAsync(int privateSessionId)
        {
            var session = await DbContext.Set<PrivateSession>().FirstOrDefaultAsync(s => s.PrivateSessionID == privateSessionId);
            if (session == null)
            {
                throw new KeyNotFoundException("No private session found with this id.");
            }

            if (session.Status != PrivateSessionStatus.Scheduled)
            {
                throw new BusinessRuleException("Only scheduled sessions can be marked as completed.");
            }

            session.Status = PrivateSessionStatus.Completed;
            DbContext.Set<PrivateSession>().Update(session);
            var affected = await DbContext.SaveChangesAsync();

            return affected > 0;
        }
        private async Task EnsureParticipantsAvailableAsync(int trainerId, int memberProfileId, DateTime sessionDate, TimeSpan startTime, TimeSpan endTime, int? excludingSessionId)
        {
            var conflict = await DbContext.Set<PrivateSession>().AnyAsync(s =>
                s.SessionDate == sessionDate &&
                s.Status == PrivateSessionStatus.Scheduled && 
                (excludingSessionId == null || s.PrivateSessionID != excludingSessionId) &&
                s.StartTime < endTime && startTime < s.EndTime &&
                (s.TrainerID == trainerId || s.MemberUserId == memberProfileId)); 

            if (conflict)
            {
                throw new BusinessRuleException("The trainer or the member already has an active session that overlaps this time slot.");
            }
        }
        private async Task EnsureTrainerAvailableAsync(int trainerId, DateTime sessionDate, TimeSpan startTime, TimeSpan endTime, int? excludingSessionId)
        {
            var conflict = await DbContext.Set<PrivateSession>().AnyAsync(s =>
                s.TrainerID == trainerId &&
                s.SessionDate == sessionDate &&
                s.Status == PrivateSessionStatus.Scheduled &&
                (excludingSessionId == null || s.PrivateSessionID != excludingSessionId) &&
                s.StartTime < endTime && startTime < s.EndTime);

            if (conflict)
            {
                throw new BusinessRuleException("The trainer already has a private session that overlaps this time slot.");
            }
        }
        public async Task<PaginationResponseDto<PrivateSessionDto>> GetAllSessionsAsync(int page, int pageSize, PrivateSessionStatus? status)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 50) pageSize = 20;

            var query = DbContext.Set<PrivateSession>()
                .Include(s => s.Trainer).ThenInclude(t => t.User)
                .Include(s => s.MemberProfile).ThenInclude(m => m.User)
                .Where(s => !s.IsDeleted)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            var totalCount = await query.CountAsync();

            var sessions = await query
                .OrderByDescending(s => s.SessionDate)
                .ThenByDescending(s => s.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = sessions.Select(s => MapToDto(s, s.Trainer.User.FullName, s.MemberProfile.User.FullName)).ToList();

            return new PaginationResponseDto<PrivateSessionDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = data
            };
        }
        private static PrivateSessionDto MapToDto(PrivateSession session, string trainerName, string memberName)
        {
            return new PrivateSessionDto
            {
                PrivateSessionID = session.PrivateSessionID,
                TrainerID = session.TrainerID,
                TrainerName = trainerName,
                MemberUserId = session.MemberUserId,
                MemberName = memberName,
                SessionDate = session.SessionDate,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Status = session.Status,
                Notes = session.Notes,
            };
        }
    }
}
