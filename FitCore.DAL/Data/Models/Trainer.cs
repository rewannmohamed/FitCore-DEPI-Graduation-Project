using FitCore.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Models
{
    public class Trainer : IAuditable,ISoftDelete
    {
        public int TrainerID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public string Specialization { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string WorkingHours { get; set; } = string.Empty;

        public ICollection<Class> Classes { get; set; } = new List<Class>();
        public ICollection<TrainerWorkingHour> WorkingHoursSchedule { get; set; } = new List<TrainerWorkingHour>();
        public ICollection<PrivateSession> PrivateSessions { get; set; } = new List<PrivateSession>();
        public bool IsDeleted { get; set ; }
        public DateTime? DeletedAt { get ; set ; }
    }
}
