using FitCore.Shared.Enums;

namespace FitCore.Shared.DTOs.Trainers
{
    public class StaffDto
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserRoles Role { get; set; }

        // Only populated when Role == Trainer
        public int? TrainerID { get; set; }
        public string? Specialization { get; set; }
        public string? Bio { get; set; }
        public ICollection<TrainerWorkingHourDto> WorkingHours { get; set; } = new List<TrainerWorkingHourDto>();
    }
}
