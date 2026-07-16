using System.Collections.Generic;

namespace FitCore.Shared.DTOs.Trainers
{
    public class TrainerDto
    {
        public int TrainerID { get; set; }
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public ICollection<TrainerWorkingHourDto> WorkingHours { get; set; } = new List<TrainerWorkingHourDto>();
    }
}
