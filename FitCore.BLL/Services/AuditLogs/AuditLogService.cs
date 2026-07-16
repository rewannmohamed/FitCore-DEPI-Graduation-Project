using FitCore.BLL.Interfaces.AuditLogs;
using FitCore.DAL.Data.Contexts;
using FitCore.DAL.Data.Models;
using FitCore.DAL.Interfaces;
using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.AuditLogs;
using FitCore.Shared.Enums;
using Microsoft.EntityFrameworkCore;


namespace FitCore.BLL.Services.AuditLogs
{
    public class AuditLogService(FitCoreDbContext DbContext) : IAuditLogsService
    {
        public async Task<PaginationResponseDto<AuditLogsDto>> ViewAllAudits
            (int page, int pageSize, string? searchTerm = null,
            AuditSortBy sortBy = AuditSortBy.Date , bool isDescending = true)
        {
            if (page <= 0) page = 1;

            const int maxPageSize = 20;

            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var query = DbContext.Set<AuditLog>().Include(x => x.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                
                query = query.Where(x =>
                    x.EntityName.ToLower().Contains(lowerSearch) ||
                    x.Action.ToLower().Contains(lowerSearch) ||
                    (x.User.FullName != null && x.User.FullName.ToLower().Contains(lowerSearch))
                );
            }

            int count = await query.CountAsync();

            var logs = query.Select(a => new AuditLogsDto
            {
                Action = a.Action,
                EntityName = a.EntityName,
                CreatedAt = a.CreatedAt,
                EntityPrimaryKey = a.EntityPrimaryKey,
                Id = a.Id,
                NewValue = a.NewValue,
                OldValue = a.OldValue,
                UserId = a.UserId ?? 0,
                UserName = a.User.FullName ?? "System",

            });

            logs = sortBy switch
            {
                AuditSortBy.Action => isDescending ? logs.OrderByDescending(x => x.Action) : logs.OrderBy(x => x.Action),
                AuditSortBy.Entity => isDescending ? logs.OrderByDescending(x => x.EntityName) : logs.OrderBy(x => x.EntityName),
                AuditSortBy.User => isDescending ? logs.OrderByDescending(x => x.UserName) : logs.OrderBy(x => x.UserName),
                AuditSortBy.Date => isDescending ? logs.OrderByDescending(x => x.CreatedAt) : logs.OrderBy(x => x.CreatedAt),
                _ => isDescending ? logs.OrderByDescending(x => x.CreatedAt) : logs.OrderBy(x => x.CreatedAt)
            };

            var data = await logs

                .OrderByDescending(a => a.CreatedAt)
                .ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToListAsync();

            return new PaginationResponseDto<AuditLogsDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = count,
                Data = data
            };
        }
    }
}
