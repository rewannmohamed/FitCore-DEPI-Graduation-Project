using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.Notification
{
    public class RequestNotificationDto
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public ICollection<UserRoles> RecieveUserRoles { get; set; } = new List<UserRoles>();
    }
}
