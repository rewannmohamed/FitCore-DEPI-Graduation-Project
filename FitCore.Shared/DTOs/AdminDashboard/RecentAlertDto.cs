using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.AdminDashboard
{
    public class RecentAlertDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty; // سنحسبها في الـ Service
        public string Type { get; set; } = string.Empty;
    }
}
