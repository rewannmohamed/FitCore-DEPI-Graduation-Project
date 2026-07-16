using System.Collections.Generic;

namespace FitCore.Shared.DTOs.Trainers
{
    public class SetWorkingHoursDto
    {
        public ICollection<TrainerWorkingHourDto> WorkingHours { get; set; } = new List<TrainerWorkingHourDto>();
    }
}
