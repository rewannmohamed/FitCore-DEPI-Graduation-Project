using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.MemberDashboard
{
    public class ProfileStatsDto
    {
        public double AttendancePercentage { get; set; }
        public string MembershipStatus { get; set; } = string.Empty;
    }
}
