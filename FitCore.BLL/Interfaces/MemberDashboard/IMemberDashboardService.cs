using FitCore.Shared.DTOs.MemberDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.BLL.Interfaces.MemberDashboard
{
    public interface IMemberDashboardService
    {
        Task<ProfileStatsDto> GetProfileStatsAsync(int userId);

        Task<NextClassDto> GetNextClassAsync(int userId);


        Task<List<NotificationDto>> GetNotificationsAsync(int userId);

        Task<DigitalPassDto> GetDigitalPassAsync(int userId);
    }
}
