using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.AdminDashboard
{
    public class RecentEnrolmentDto
    {
        public string MemberName { get; set; } = string.Empty;
        public string PlanType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
    }
}
