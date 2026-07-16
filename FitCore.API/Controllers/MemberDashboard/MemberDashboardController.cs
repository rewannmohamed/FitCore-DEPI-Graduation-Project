using FitCore.BLL.Interfaces.Auth;
using FitCore.BLL.Interfaces.MemberDashboard;
using FitCore.DAL.Data.Models;
using FitCore.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.API.Controllers.MemberDashboard
{
    [Route("api/member/[controller]")]
    [ApiController]
    //[Authorize(Roles = nameof(UserRoles.Member))]
    [Authorize]
    public class MemberDashboardController : ControllerBase
    {
        private readonly IMemberDashboardService _service;
        private readonly ICurrentUserService _currentUser;

        public MemberDashboardController(IMemberDashboardService service, ICurrentUserService currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            int userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
            var result = await _service.GetProfileStatsAsync(userId);
            return Ok(result);
        }

        [HttpGet("next-class")]
        [Authorize]
        public async Task<IActionResult> GetNextClass()
        {
            int userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
            var result = await _service.GetNextClassAsync(userId);
            return Ok(result);
        }



        [HttpGet("notifications")]
        [Authorize]
        public async Task<IActionResult> GetNotifications([FromQuery] int userId) => Ok(await _service.GetNotificationsAsync(userId));

        [HttpGet("digital-pass")]
        public async Task<IActionResult> GetDigitalPass()
        {
            int userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
            var result = await _service.GetDigitalPassAsync(userId);
            return Ok(result);
        }  
    }
}
