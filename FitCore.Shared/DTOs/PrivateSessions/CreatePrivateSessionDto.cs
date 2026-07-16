using System;

namespace FitCore.Shared.DTOs.PrivateSessions
{
    public class CreatePrivateSessionDto
    {
        public int TrainerID { get; set; }
        public int MemberUserId { get; set; }
        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
