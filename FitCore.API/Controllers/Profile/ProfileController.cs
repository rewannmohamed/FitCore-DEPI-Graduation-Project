using FitCore.BLL.Interfaces.Profile;
using FitCore.Shared.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.API.Controllers.Profile
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController(IProfileService _profileService) : ControllerBase
    {
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _profileService.GetProfile();
            return Ok(result);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateProfile(EditUserDto editUserDto)
        {
            await _profileService.EditProfile(editUserDto);

            return Ok(new { Message = "Profile updated successfully" });
        }
    }
}
