using System.Collections.Generic;

namespace FitCore.Shared.DTOs.Classes
{
    public class CreateClassDto
    {
        public string ClassName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public decimal Price { get; set; }
        public int TrainerID { get; set; }
        public int NumberOfSessions { get; set; }

        public ICollection<ClassScheduleDto> Schedules { get; set; } = new List<ClassScheduleDto>();
    }
}
