using FitCore.DAL.Interfaces;
using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Models
{
    public class Class : IAuditable,ISoftDelete
    {
        public int ClassID { get; set; }
        public int TrainerID { get; set; }
        public Trainer Trainer { get; set; } = null!;

        public string ClassName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Capacity { get; set; }
        public int NumberOfSessions { get; set; } = 1;
        public ClassStatus Status { get; set; }

        public ICollection<ClassSchedule> Schedules { get; set; } = new List<ClassSchedule>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
        public ICollection<InvoiceItem> InvoicesItems { get; set; } = new List<InvoiceItem>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public bool IsDeleted { get; set ; }
        public DateTime? DeletedAt { get; set; }
    }
}
