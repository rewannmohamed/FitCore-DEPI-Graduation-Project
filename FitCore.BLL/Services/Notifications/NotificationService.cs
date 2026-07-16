using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces.Auth;
using FitCore.BLL.Interfaces.Notifications;
using FitCore.DAL.Data.Contexts;
using FitCore.DAL.Data.Models;
using FitCore.DAL.Interfaces;
using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.Notification;
using FitCore.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitCore.BLL.Services.Notifications
{
    public class NotificationService(FitCoreDbContext DbContext,ICurrentUserService _currentService) : INotificationService
    {
        public async Task<PaginationResponseDto<NotificationDto>> GetAllNotifications(int page, int pageSize)
        {
            //int userId = 2;
            int userId = _currentService.UserId ?? throw new UnauthorizedAccessException("No user id assigned");
            if (page <= 0) page = 1;

            const int maxPageSize = 20;

            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var query =DbContext.Set<Notification>()
                .OrderByDescending(x => x.CreatedAt).Where(x => x.UserID == userId);

            var rowsCount = query.Count();

            var messages = query.Skip((page - 1) * pageSize)
                .Take(pageSize);

            var messageDtos = await messages.Select(x => new NotificationDto
            {
                Id = x.NotificationID,
                Title = x.Title,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt,
                Message = x.Content,
                Type = x.Type,
                ActionUrl = x.ActionUrl,
            }).ToListAsync();


            return new PaginationResponseDto<NotificationDto>()
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = rowsCount,
                Data = messageDtos
            };
        }
        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            int userId = _currentService.UserId ?? throw new UnauthorizedAccessException("No user id assigned");
            //int userId = 2;

            var notification = await DbContext.Notifications.FirstOrDefaultAsync(x => x.NotificationID == notificationId);


            if (notification == null || notification.UserID != userId) throw new KeyNotFoundException("no notification with this id");

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                DbContext.Notifications.Update(notification);
                await DbContext.SaveChangesAsync();
            }

            return true;
        }

        public async Task MarkAllAsReadAsync()
        {
            //int userId = 2;
            int userId = _currentService.UserId ?? throw new UnauthorizedAccessException("No branch id assigned");

            var unreadNotifications = await DbContext.Set<Notification>()
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    DbContext.Set<Notification>().Update(notification);                    
                }

                await DbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> SendNotification(RequestNotificationDto notificationDto)
        {
            //int userId = 3;
            int userId = _currentService.UserId ?? throw new UnauthorizedAccessException("No user id assigned");
            
            var SentUserRoles =await DbContext.Set<User>().Where(x => x.UserID == userId)
                .Select(x=> x.UserRoles)
                .FirstOrDefaultAsync();
            
            if (SentUserRoles == null)
            {
                return false;
            }
            
            foreach (var role in SentUserRoles)
            {
                if (role.Role == UserRoles.Member)
                {
                    throw new BusinessRuleException("Member can't push notifications");
                }
            }

            if (notificationDto == null)
            {
                throw new ArgumentNullException("Notification fields are empty, please fill required fields");
            }

            var users = DbContext.Set<User>().Include(x=> x.UserRoles).AsQueryable();
           
            foreach (var user in users)
            { 
                foreach(var UserRole in user.UserRoles)
                {
                    foreach (var role in notificationDto.RecieveUserRoles)
                    {
                        if (role == UserRole.Role)
                        {                           
                            Notification notification = new Notification()
                            {
                                CreatedAt = DateTime.UtcNow,
                                Content = notificationDto.Message,
                                Title = notificationDto.Title,
                                IsRead = false,
                                Type = NotificationTypeEnum.Announcement,
                                UserID = user.UserID,
                            };
                            await DbContext.Set<Notification>().AddAsync(notification);
                        }
                    }
                }
            }
            
            int affectedRows= await DbContext.SaveChangesAsync();

            if (affectedRows <= 0)
            {
                return false;
            }
            return true;
            
        }

        public async Task MemberExpiryNotification()
        {
            var memberShips = await DbContext.Set<Membership>()
                .ToListAsync();

            foreach (var membership in memberShips)
            {
                if(membership.EndDate <= DateTime.UtcNow)
                {
                    Notification notification = new Notification()
                    {
                        CreatedAt = DateTime.UtcNow,
                        Content = "Your MemberShip has been expired",
                        Title = "MemberShip Expiration",
                        IsRead = false,
                        Type = NotificationTypeEnum.MembershipExpiration,
                        UserID = membership.MemberProfile.UserID,
                        ActionUrl = $"/html/user/Memberships/member-ship-details.html?id={membership.MembershipID}"
                    };
                    await DbContext.Set<Notification>().AddAsync(notification);
                }
            }
            int affectedRows = await DbContext.SaveChangesAsync();
        }

        public async Task LowStockNotification()
        {
            var lowStockProducts = await DbContext.Products
            .Select(p => new
            {
                ProductId = p.ProductID,
                ProductName = p.Name,
                p.ReorderLevel,
                TotalQuantity = p.Inventories.Sum(i => i.Quantity)
            })
            .Where(x => x.TotalQuantity <= x.ReorderLevel)
            .ToListAsync();


            var adminIds = await DbContext.Users
            .Where(u => u.UserRoles.Any(ur => ur.Role == UserRoles.Admin))
            .Select(u => u.UserID)
            .ToListAsync();

            if (!adminIds.Any())
                return;

            foreach (var adminId in adminIds)
            {
                foreach (var product in lowStockProducts)
                {
                    Notification notification = new Notification()
                    {
                        Title = "Low Stock",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        UserID = adminId,
                        Content = $"The Product: {product.ProductName} is under reorder level.",
                        Type = NotificationTypeEnum.LowStock
                    };
                    await DbContext.Notifications.AddAsync(notification);
                }
            }

            await DbContext.SaveChangesAsync();

        }

        public async Task ExpiryProductsNotification()
        {
            var expiryThreshold = DateTime.UtcNow.AddDays(30);

            var expiringInventories = await DbContext.Inventories
                .Include(i => i.Product)
                .Where(i => i.ExpiryDate != null && i.ExpiryDate <= expiryThreshold)
                .ToListAsync();

            if (!expiringInventories.Any())
                return;

            var adminIds = await DbContext.Users
                .Where(u => u.UserRoles.Any(ur => ur.Role == UserRoles.Admin))
                .Select(u => u.UserID)
                .ToListAsync();

            if (!adminIds.Any())
                return;

            var notificationsToInsert = new List<Notification>();

            foreach (var item in expiringInventories)
            {
                foreach (var adminId in adminIds)
                {
                    notificationsToInsert.Add( new Notification
                    {
                        UserID = adminId,
                        Title = "Product Near Expiry",
                        Content = $"The Product: {item.Product.Name} will expire in {item.ExpiryDate?.ToString("yyyy-MM-dd")}. Available Quantity from this product with this expiry" +
                        $": {item.Quantity}.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        Type = NotificationTypeEnum.productExpiry,
                    });
                }
            }

            await DbContext.Notifications.AddRangeAsync(notificationsToInsert);
            await DbContext.SaveChangesAsync();
        }

        public async Task<int> GetUnReadNotificationsCount()
        {
            //int userId = 2;
            int userId = _currentService.UserId ?? throw new UnauthorizedAccessException("No branch id assigned");

            int count = await DbContext.Notifications
             .Where(n => !n.IsRead && n.UserID == userId)
            .CountAsync();

            return count;
        }
    }
}
