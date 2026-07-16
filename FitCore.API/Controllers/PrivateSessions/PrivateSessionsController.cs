using FitCore.BLL.Interfaces.PrivateSessions;
using FitCore.Shared.DTOs.PrivateSessions;
using FitCore.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.API.Controllers.PrivateSessions
{
    [ApiController]
    [Route("api/[controller]")]
    
    public class PrivateSessionsController(IPrivateSessionService privateSessionService) : ControllerBase
    {
        [HttpPost]
        [Authorize(Roles = nameof(UserRoles.Admin) + "," + nameof(UserRoles.Receptionist))]
        public async Task<IActionResult> CreatePrivateSession(CreatePrivateSessionDto dto)
        {
            var result = await privateSessionService.CreatePrivateSessionAsync(dto);
            return Ok(result);
        }

        [HttpPut("{privateSessionId}/assign-trainer/{trainerId}")]
        [Authorize(Roles = nameof(UserRoles.Admin) + "," + nameof(UserRoles.Receptionist))]
        public async Task<IActionResult> AssignTrainer(int privateSessionId, int trainerId)
        {
            var result = await privateSessionService.AssignTrainerAsync(privateSessionId, trainerId);
            return Ok(result);
        }

        [HttpGet("trainer/{trainerId}")]
        public async Task<IActionResult> GetSessionsByTrainer(int trainerId)
        {
            var result = await privateSessionService.GetSessionsByTrainerAsync(trainerId);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = nameof(UserRoles.Admin) + "," + nameof(UserRoles.Receptionist))]
        public async Task<IActionResult> GetAllSessions(
            [FromQuery(Name = "Page")] int page = 1,
            [FromQuery(Name = "Page_Size")] int pageSize = 20,
            [FromQuery] PrivateSessionStatus? status = null)
        {
            var result = await privateSessionService.GetAllSessionsAsync(page, pageSize, status);
            return Ok(result);
        }

        [HttpGet("member/{memberUserId}")]
        public async Task<IActionResult> GetSessionsByMember(int memberUserId)
        {
            var result = await privateSessionService.GetSessionsByMemberAsync(memberUserId);
            return Ok(result);
        }

        [HttpPatch("{privateSessionId}/cancel")]
        public async Task<IActionResult> CancelSession(int privateSessionId)
        {
            var result = await privateSessionService.CancelSessionAsync(privateSessionId);
            if (!result) return BadRequest();

            return Ok(new { Message = "Private session cancelled." });
        }

        [HttpPatch("{privateSessionId}/complete")]
        public async Task<IActionResult> CompleteSession(int privateSessionId)
        {
            var result = await privateSessionService.CompleteSessionAsync(privateSessionId);
            if (!result) return BadRequest();

            return Ok(new { Message = "Private session marked as completed." });
        }
    }
}
