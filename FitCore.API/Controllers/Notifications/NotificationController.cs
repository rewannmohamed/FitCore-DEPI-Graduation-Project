using FitCore.BLL.Interfaces.Notifications;
using FitCore.Shared.DTOs.Notification;
using FitCore.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.API.Controllers.Notifications
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController (INotificationService _notificationService) : ControllerBase
    {
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetNotifications([FromQuery(Name = "Page_Size")] int pageSize = 20, [FromQuery(Name = "Page")] int page = 1)
        {
            var result = await _notificationService.GetAllNotifications(page, pageSize);
            return Ok(result);
        }
        [HttpPatch("mark-as-read/{id}")]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var result = await _notificationService.MarkAsReadAsync(id);
            if (!result) return NotFound("Notification not found.");

            return Ok(new { Message = "Notification marked as read." });
        }

        [HttpPatch("mark-all-read")]
        [Authorize]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync();

            return Ok(new { Message = "All notifications marked as read." });
        }

        [Authorize(Roles = nameof(UserRoles.Admin))]
        [HttpPost]
        public async Task<IActionResult> PushNotification(RequestNotificationDto notificationDto)
        {
            var result = await _notificationService.SendNotification(notificationDto);
            if(result == false)
            {
                return BadRequest();
            }
            
            return Ok(result);
        }

        [Authorize]
        [HttpGet("UnRead-Count")]
        public async Task<IActionResult> GetUnReadCount()
        {
            var result = await _notificationService.GetUnReadNotificationsCount();
            return Ok(result);
        }
    }
}
