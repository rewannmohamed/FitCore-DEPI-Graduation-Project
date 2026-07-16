using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.Notification;
using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.BLL.Interfaces.Notifications
{
    public interface INotificationService
    {
        public Task<PaginationResponseDto<NotificationDto>> GetAllNotifications(int page, int pageSize);
        public Task<bool> MarkAsReadAsync(int notificationId);
        public Task MarkAllAsReadAsync();
        public Task<bool> SendNotification(RequestNotificationDto notificationDto);

        public Task MemberExpiryNotification();
        public Task LowStockNotification();
        public Task ExpiryProductsNotification();

        public Task<int> GetUnReadNotificationsCount();

    }
}
