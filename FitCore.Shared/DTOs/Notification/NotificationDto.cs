using FitCore.Shared.Enums;

namespace FitCore.Shared.DTOs.Notification
{
   public class NotificationDto
   {
        public int Id { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationTypeEnum Type { get; set; }
        public string? ActionUrl { get; set; }
    }
}
