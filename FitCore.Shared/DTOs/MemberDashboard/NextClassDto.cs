using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.MemberDashboard
{
    public class NextClassDto
    {
        public string ClassName { get; set; } = string.Empty;
        public string StudioName { get; set; } = string.Empty;
        public string TrainerName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }
}
