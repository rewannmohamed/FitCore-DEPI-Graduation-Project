using FitCore.BLL.Interfaces.AuditLogs;
using FitCore.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Tanzeem.Presentation.AuditLogs
{
    [ApiController]
    [Route("api/[controller]")]

    public class AuditLogsController(IAuditLogsService auditLogsService) : ControllerBase
    {
        [HttpGet]
        //[Authorize(Roles = nameof(UserRoles.Admin))]
        [Authorize]
        public async Task<IActionResult> GetAudits([FromQuery(Name = "Page_Size")] int pageSize = 15, [FromQuery(Name = "Page")] int page = 1,
            string? searchTerm = null,
            AuditSortBy sortBy = AuditSortBy.Date,
            bool isDescending = true)
        {
            var result = await auditLogsService.ViewAllAudits(page, pageSize,searchTerm,sortBy,isDescending);
            return Ok(result);
        }
    }
}