using FitCore.Shared.Enums;
using System;

namespace FitCore.Shared.DTOs.PrivateSessions
{
    public class PrivateSessionDto
    {
        public int PrivateSessionID { get; set; }

        public int TrainerID { get; set; }
        public string TrainerName { get; set; } = string.Empty;

        public int MemberUserId { get; set; }
        public string MemberName { get; set; } = string.Empty;

        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public PrivateSessionStatus Status { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
