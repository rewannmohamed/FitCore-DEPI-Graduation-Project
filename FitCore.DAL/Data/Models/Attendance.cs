using FitCore.DAL.Interfaces;
using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Models
{
    public class Attendance : ISoftDelete
    {
        public int AttendanceID { get; set; }
        public int UserId { get; set; }
        public MemberProfile MemberProfile { get; set; } = null!;

        public int? MembershipID { get; set; }
        public Membership? Membership { get; set; }

        public AttendenceType Type { get; set; }
        public DateTime CheckInTime { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
