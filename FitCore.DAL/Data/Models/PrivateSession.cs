using FitCore.DAL.Interfaces;
using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Models
{
    public class PrivateSession : IAuditable,ISoftDelete
    {
        public int PrivateSessionID { get; set; }

        public int TrainerID { get; set; }
        public Trainer Trainer { get; set; } = null!;

        public int MemberUserId { get; set; }
        public MemberProfile MemberProfile { get; set; } = null!;

        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public PrivateSessionStatus Status { get; set; }
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
