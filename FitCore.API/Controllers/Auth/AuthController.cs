using FitCore.BLL.Interfaces.Auth;
using FitCore.BLL.Services.Auth;
using FitCore.Shared.DTOs.Auth;
using FitCore.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.API.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService _authService, ICurrentUserService _currentUserService) : ControllerBase
    {
        
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var result = await _authService.Login(loginDto);
            return Ok(result);
        }

        
        [HttpPost("register-member")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterMember(RegisterMemberDto dto)
        {
            var result = await _authService.RegisterMember(dto);
            return Ok(result);
        }

       
        [HttpPost("create-staff")]
        [Authorize(Roles = "Admin")]
        [Authorize()]
        public async Task<IActionResult> CreateStaff(CreateStaffDto dto)
        {
            var result = await _authService.CreateStaff(dto);
            return Ok(result);
        }

        
        [HttpPut("promote-to-trainer/{userId:int}")]
        [Authorize()]
        public async Task<IActionResult> PromoteToTrainer(int userId)
        {
            await _authService.PromoteMemberToTrainer(userId);
            return Ok(new SimpleMessageDto { Message = "User promoted to Trainer successfully." });
        }

        
        [HttpGet("users")]
        [Authorize(Roles = "Receptionist,Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _authService.GetAllUsers();
            return Ok(result);
        }


        [HttpPost("role-change/request")]
        [Authorize]
        public async Task<IActionResult> RequestRoleChange(RequestRoleChangeDto dto)
        {
            var userId = _currentUserService.GetRequiredUserId();
            var result = await _authService.RequestRoleChange(userId, dto.RequestedRole);
            return Ok(result);
        }


        [HttpGet("role-change/pending")]
        [Authorize(Roles = "Admin")]
        [Authorize()]
        public async Task<IActionResult> GetPendingRoleChangeRequests()
        {
            var result = await _authService.GetPendingRoleChangeRequests();
            return Ok(result);
        }


        [HttpPut("role-change/{requestId:int}/approve")]
        [Authorize(Roles = "Admin")]
        [Authorize()]
        public async Task<IActionResult> ApproveRoleChange(int requestId, ReviewRoleChangeDto dto)
        {
            var adminId = _currentUserService.GetRequiredUserId();
            await _authService.ApproveRoleChangeRequest(requestId, adminId, dto?.Note);
            return Ok(new SimpleMessageDto { Message = "Role change request approved." });
        }


        [HttpPut("role-change/{requestId:int}/reject")]
        [Authorize(Roles = "Admin")]
        [Authorize()]
        public async Task<IActionResult> RejectRoleChange(int requestId, ReviewRoleChangeDto dto)
        {
            var adminId = _currentUserService.GetRequiredUserId();
            await _authService.RejectRoleChangeRequest(requestId, adminId, dto?.Note);
            return Ok(new SimpleMessageDto { Message = "Role change request rejected." });
        }


        [AllowAnonymous]
        [HttpPost("setup-admin")]
        public async Task<IActionResult> CreateAdmin([FromQuery] string secretKey, [FromBody] RegisterMemberDto dto)
        {
            var result = await _authService.CreateAdmin(dto, secretKey);
            return Ok(result);
        }
    }
}
