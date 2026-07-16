using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces.Auth;
using FitCore.BLL.Interfaces.GymService;
using FitCore.BLL.Services.Classes;
using FitCore.DAL.Data.Models;
using FitCore.Shared.DTOs.GymService;
using FitCore.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GymServicesController(IGymServiceService _gymService,ICurrentUserService _currentUser) : ControllerBase
    {
        [Authorize]
        [HttpPost("bookings")]
        public async Task<IActionResult> AddGymServiceToBooking([FromQuery] int gymServiceId)
        {
            int userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
            try
            {
                var result = await _gymService.AddGymServiceToBookingAsync(userId, gymServiceId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (BusinessRuleException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateGymService([FromBody] CreateGymServiceDto dto)
        {
            try
            {
                var result = await _gymService.CreateGymServiceAsync(dto);
                return CreatedAtAction(nameof(GetGymServices), new { id = result.ServiceID }, result);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id}")]

        public async Task<IActionResult> UpdateGymService(int id, [FromBody] UpdateGymServiceDto dto)
        {
            try
            {
                var result = await _gymService.UpdateGymServiceAsync(id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]

        public async Task<IActionResult> DeleteGymService(int id)
        {
            try
            {
                await _gymService.DeleteGymServiceAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet]

        public async Task<IActionResult> GetGymServices(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] ServiceCategory? category = null)
        {
            var result = await _gymService.GetGymServicesAsync(page, pageSize, searchTerm, category);
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("bookings/{bookingId}/cancel")]

        public async Task<IActionResult> CancelGymServiceBooking(int bookingId)
        {
            int userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

            try
            {
                await _gymService.CancelGymServiceBookingAsync(userId, bookingId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (BusinessRuleException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("book")]
        public async Task<IActionResult> BookGymService([FromQuery] int gymServiceId)
        {
            try
            {
                int userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

                var result = await _gymService.AddGymServiceToBookingAsync(userId, gymServiceId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (BusinessRuleException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("my-services")]
        public async Task<IActionResult> GetMyServiceBookings()
        {
            int userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

            var result = await _gymService.GetMemberGymServiceBookingsAsync(userId);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("admin/users/{userId}/bookings")]
        public async Task<IActionResult> GetUserServiceBookings(int userId)
        {
            var result = await _gymService.GetMemberGymServiceBookingsAsync(userId);
            return Ok(result);
        }

        [Authorize]
        [HttpPatch("/admin/bookings/{bookingId}/cancel/{userId}")]
        public async Task<IActionResult> CancelServiceBooking(int userId, int bookingId)
        {
           await _gymService.CancelGymServiceBookingAsync(userId, bookingId);
            return Ok(1);
        }

        [Authorize]
        [HttpPost("admin/book")]
        public async Task<IActionResult> BookAdminGymService([FromQuery] int userId, [FromQuery] int gymServiceId)
        {
            var result = await _gymService.AddGymServiceToBookingAsync(userId, gymServiceId);
            return Ok(result);
        }
        [Authorize]
        [HttpGet("bookings")]
        public async Task<IActionResult> GetAllBookings()
        {
            int userId = _currentUser?.UserId ?? throw new UnauthorizedAccessException();
            var result = await _gymService.GetAllBookingsAsync(userId);
            return Ok(result);
        }
    }
}