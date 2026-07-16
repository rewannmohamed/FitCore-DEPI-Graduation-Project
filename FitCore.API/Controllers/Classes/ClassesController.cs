using FitCore.BLL.Interfaces.Classes;
using FitCore.DAL.Data.Models;
using FitCore.Shared.DTOs.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FitCore.API.Controllers.Classes
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClassesController(IClassService classService) : ControllerBase
    {
  
        private int GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User token is invalid or missing.");

            return int.Parse(userIdClaim);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateClass(CreateClassDto dto)
        {
            var result = await classService.CreateClassAsync(dto);
            return CreatedAtAction(nameof(GetClassById), new { classId = result.ClassID }, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{classId}")]
        public async Task<IActionResult> UpdateClass(int classId, UpdateClassDto dto)
        {
            var result = await classService.UpdateClassAsync(classId, dto);
            return Ok(result);
        }

  
        [Authorize(Roles = "Admin")]
        [HttpDelete("{classId}")]
        public async Task<IActionResult> DeleteClass(int classId)
        {
            var result = await classService.DeleteClassAsync(classId);
            if (!result) return BadRequest();

            return Ok(new { Message = "Class deleted." });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllClasses([FromQuery(Name = "Page_Size")] int pageSize = 20, [FromQuery(Name = "Page")] int page = 1)
        {
            var result = await classService.GetAllClassesAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("{classId}")]
        public async Task<IActionResult> GetClassById(int classId)
        {
            var result = await classService.GetClassByIdAsync(classId);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{classId}/schedules")]
        public async Task<IActionResult> AddSchedule(int classId, ClassScheduleDto dto)
        {
            var result = await classService.AddScheduleAsync(classId, dto);
            return Ok(result);
        }


        [HttpGet("browse")]
        public async Task<IActionResult> BrowseClasses(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery(Name = "Page_Size")] int pageSize = 20,
            [FromQuery(Name = "Page")] int page = 1)
        {
            var from = fromDate ?? DateTime.UtcNow.Date;
            var to = toDate ?? from.AddDays(14);

            var result = await classService.BrowseClassesAsync(from, to, page, pageSize);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("book")]
        public async Task<IActionResult> BookClass(int classId)
        {
            var memberUserId = GetUserIdFromToken();
            var result = await classService.BookClassAsync(memberUserId, classId);
            return Ok(result);
        }

        [Authorize]
        [HttpPatch("bookings/{bookingId}/cancel")]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var memberUserId = GetUserIdFromToken();
            var result = await classService.CancelBookingAsync(memberUserId, bookingId);
            if (!result) return BadRequest();

            return Ok(new { Message = "Booking cancelled." });
        }

        [Authorize]
        [HttpGet("my-bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            var memberUserId = GetUserIdFromToken();
            var result = await classService.GetMemberBookingsAsync(memberUserId);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("admin/users/{userId}/bookings")]
        public async Task<IActionResult> GetUserBookings(int userId)
        {
            var result = await classService.GetMemberBookingsAsync(userId);
            return Ok(result);
        }


        [Authorize]
        [HttpPatch("admin/bookings/{bookingId}/cancel/{currentMemberUserId}")]
        public async Task<IActionResult> CancelBookingAsync(int currentMemberUserId, int bookingId)
        {
            var result = await classService.CancelBookingAsync(currentMemberUserId,bookingId);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("admin/book")]
        public async Task<IActionResult> BookAdminClass([FromQuery] int memberUserId, [FromQuery] int classId)
        {
            var result = await classService.BookClassAsync(memberUserId, classId);
            return Ok(result);
        }

    }

}
