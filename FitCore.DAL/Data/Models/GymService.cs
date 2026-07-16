using FitCore.DAL.Interfaces;
using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Models
{
    public class GymService : ISoftDelete
    {
        public int ServiceID { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public ServiceCategory Category { get; set; }

        public int DurationInDays { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int AllowedSessionsCount { get; set; }
        public ICollection<InvoiceItem> InvoicesItems { get; set; } = new List<InvoiceItem>();
        public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        
    }
}
