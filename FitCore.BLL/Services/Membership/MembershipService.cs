using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces;
using FitCore.BLL.Interfaces.Membership;
using FitCore.DAL.Data.Contexts;
using FitCore.DAL.Data.Models;
using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.MemberShip;
using FitCore.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitCore.BLL.Services
{
    public class MembershipService(FitCoreDbContext DbContext): IMembershipService
    {
        private async Task<MembershipDto> CreateMembershipAsync(CreateMembershipDto dto)
        {
            var member = await DbContext.Set<MemberProfile>().FirstOrDefaultAsync(m => m.MemberProfileId == dto.MemberProfileId);
            if (member == null) throw new KeyNotFoundException("Member profile not found.");

            if (dto.GymServiceId == null && dto.ClassId == null)
                throw new ValidationException("You must provide either a GymServiceId or a ClassId.");

            var startDate = DateTime.UtcNow;
            DateTime endDate;
            int? remainingSessions = null;
            Membership membership;
            
            if (dto.GymServiceId.HasValue)
            {
                var service = await DbContext.Set<GymService>().FirstOrDefaultAsync(s => s.ServiceID == dto.GymServiceId.Value);
                if (service == null) throw new KeyNotFoundException("Gym Service not found.");

                endDate = startDate.AddDays(service.DurationInDays);
                remainingSessions = service.AllowedSessionsCount;

                membership = new Membership
                {
                    MemberProfileId = member.MemberProfileId,

                    GymServiceId = dto.GymServiceId,
                    ClassID = null,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = MemberShipStatus.Active,
                    RemainingSessions = remainingSessions,
                    IsAutoRenew = dto.IsAutoRenew,
                    CreatedAt = DateTime.UtcNow,
                    InvoiceID = dto.InvoiceId,
                };
            }
            else
            {
                var gymClass = await DbContext.Set<Class>().FirstOrDefaultAsync(c => c.ClassID == dto.ClassId.Value);
                if (gymClass == null) throw new KeyNotFoundException("Class not found.");

                endDate = startDate.AddDays(30);
                remainingSessions = gymClass.NumberOfSessions;

                membership = new Membership
                {
                    MemberProfileId = member.MemberProfileId,

                    GymServiceId = null,
                    ClassID = dto.ClassId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = MemberShipStatus.Active,
                    RemainingSessions = remainingSessions,
                    IsAutoRenew = dto.IsAutoRenew,
                    CreatedAt = DateTime.UtcNow,
                    InvoiceID = dto.InvoiceId,
                };
            }

            await DbContext.Set<Membership>().AddAsync(membership);
            await DbContext.SaveChangesAsync();

            return await GetMembershipByIdAsync(membership.MembershipID);
        }

        public async Task<ICollection<MembershipDto>> GetMemberMembershipsAsync(int memberProfileId)
        {
            var memberships = await DbContext.Set<Membership>()
                .Include(m => m.GymService)
                .Include(m => m.Class)
                .Where(m => m.MemberProfileId == memberProfileId)
                .OrderByDescending(m => m.StartDate)
                .ToListAsync();

            return memberships.Select(MapToDto).ToList();
        }

        public async Task<MembershipDto> GetMembershipByIdAsync(int membershipId)
        {
            var membership = await DbContext.Set<Membership>()
                .Include(m => m.GymService)
                .Include(m => m.Class)
                .FirstOrDefaultAsync(m => m.MembershipID == membershipId);

            if (membership == null) throw new KeyNotFoundException("Membership not found.");

            return MapToDto(membership);
        }

        public async Task<bool> FreezeMembershipAsync(int membershipId, int freezeDays)
        {
            var membership = await DbContext.Set<Membership>().FirstOrDefaultAsync(m => m.MembershipID == membershipId);

            if (membership == null) throw new KeyNotFoundException("Membership not found.");

            if (membership.Status != MemberShipStatus.Active)
                throw new BusinessRuleException("Only active memberships can be frozen.");

            if (freezeDays <= 0)
                throw new ValidationException("Freeze days must be greater than zero.");

            membership.FreezeStartDate = DateTime.UtcNow;
            membership.FreezeEndDate = DateTime.UtcNow.AddDays(freezeDays);

            membership.EndDate = membership.EndDate.AddDays(freezeDays);
            membership.Status = MemberShipStatus.Freezed;

            DbContext.Set<Membership>().Update(membership);
            var affected = await DbContext.SaveChangesAsync();

            return affected > 0;
        }
        public async Task<bool> UnfreezeMembershipAsync(int membershipId)
        {
            var membership = await DbContext.Set<Membership>().FirstOrDefaultAsync(m => m.MembershipID == membershipId);

            if (membership == null) throw new KeyNotFoundException("Membership not found.");
            if (membership.Status != MemberShipStatus.Freezed)
                throw new BusinessRuleException("Membership is not currently frozen.");

            if (membership.FreezeEndDate.HasValue && membership.FreezeEndDate.Value > DateTime.UtcNow)
            {
                var unusedFreezeDays = (membership.FreezeEndDate.Value - DateTime.UtcNow).Days;
                membership.EndDate = membership.EndDate.AddDays(-unusedFreezeDays);
            }

            membership.Status = MemberShipStatus.Active;
            membership.FreezeStartDate = null;
            membership.FreezeEndDate = null;

            DbContext.Set<Membership>().Update(membership);
            return await DbContext.SaveChangesAsync() > 0;
        }
        public async Task AutoUnfreezeExpiredFreezesAsync()
        {
            var currentDate = DateTime.UtcNow;

            var expiredFreezes = await DbContext.Set<Membership>()
                .Where(m => m.Status == MemberShipStatus.Freezed && m.FreezeEndDate <= currentDate)
                .ToListAsync();

            foreach (var membership in expiredFreezes)
            {
                membership.Status = MemberShipStatus.Active;
                membership.FreezeStartDate = null;
                membership.FreezeEndDate = null;
            }

            if (expiredFreezes.Any())
            {
                DbContext.Set<Membership>().UpdateRange(expiredFreezes);
                await DbContext.SaveChangesAsync();
            }
        }
        public async Task<PaginationResponseDto<AdminMembershipDto>> GetAllActiveMembershipsForAdminAsync(int page, int pageSize)
        {
            if (page <= 0) page = 1;
            const int maxPageSize = 50;
            if (pageSize <= 0 || pageSize > maxPageSize) pageSize = 20;

            var currentDate = DateTime.UtcNow;

            var query = DbContext.Set<Membership>()
                .Include(m => m.MemberProfile).ThenInclude(mp => mp.User)
                .Include(m => m.GymService)
                .Include(m => m.Class)
                .Where(m => m.EndDate >= currentDate && m.Status != MemberShipStatus.Expired) 
                .OrderByDescending(m => m.StartDate);

            var totalCount = await query.CountAsync();

            var memberships = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = memberships.Select(m => new AdminMembershipDto
            {
                MembershipID = m.MembershipID,
                MemberProfileId = m.MemberProfileId,

                MemberName = m.MemberProfile?.User?.FullName ?? "Unknown Member",

                MembershipType = m.GymServiceId.HasValue ? "Gym Package" : "Class Subscription",
                Name = m.GymServiceId.HasValue ? m.GymService?.Name ?? "Unknown" : m.Class?.ClassName ?? "Unknown",

                StartDate = m.StartDate,
                EndDate = m.EndDate,
                Status = m.Status,
                RemainingSessions = m.RemainingSessions
            }).ToList();

            return new PaginationResponseDto<AdminMembershipDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = data
            };
        }

        public async Task GenerateMembershipsFromInvoiceAsync(int invoiceId)
        {
            var invoice = await DbContext.Set<Invoice>()
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId);

            if (invoice == null || invoice.InvoiceStatus != InvoiceStatus.Completed)
            {
                return;
            }

            foreach (var item in invoice.InvoiceItems)
            {
                int MemberProfileId = await DbContext.MemberProfiles
                    .Where(x => x.UserID == invoice.UserID)
                    .Select(x => x.MemberProfileId)
                    .FirstOrDefaultAsync();

                if (item.ServiceID.HasValue)
                {
                    var createDto = new CreateMembershipDto
                    {
                        MemberProfileId = MemberProfileId,
                        GymServiceId = item.ServiceID.Value,
                        IsAutoRenew = false,
                        InvoiceId = invoiceId
                    };
                    await CreateMembershipAsync(createDto);
                }
                else if (item.ClassID.HasValue)
                {
                    var createDto = new CreateMembershipDto
                    {
                        MemberProfileId = MemberProfileId,
                        ClassId = item.ClassID.Value,
                        IsAutoRenew = false,
                        InvoiceId = invoiceId
                    };
                    await CreateMembershipAsync(createDto);
                }
            }
        }
        private static MembershipDto MapToDto(Membership membership)
        {
            return new MembershipDto
            {
                MembershipID = membership.MembershipID,
                MembershipType = membership.GymServiceId.HasValue ? "Gym Package" : "Class Subscription",
                Name = membership.GymServiceId.HasValue ? membership.GymService?.Name ?? "Unknown" : membership.Class?.ClassName ?? "Unknown",
                StartDate = membership.StartDate,
                EndDate = membership.EndDate,
                Status = membership.Status,
                RemainingSessions = membership.RemainingSessions
            };
        }

    }
}