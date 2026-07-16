using FitCore.DAL.Interfaces;
using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Models
{
    public class Booking : IAuditable, ISoftDelete
    {
        public int BookingID { get; set; }

        public int? ClassID { get; set; }
        public Class? Class { get; set; } = null!;

        public int? GymServiceId { get; set; }
        public GymService? GymService { get; set; }

        public int MemberUserId { get; set; }
        public MemberProfile MemberProfile { get; set; } = null!;

        //public DateTime SessionDate { get; set; }

        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
