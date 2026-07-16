using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.AuditLogs;
using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.BLL.Interfaces.AuditLogs
{
    public interface IAuditLogsService
    {
        public Task<PaginationResponseDto<AuditLogsDto>> ViewAllAudits(int page, int pageSize, string? searchTerm = null, AuditSortBy sortBy = AuditSortBy.Date, bool isDescending = true);
    }
}
