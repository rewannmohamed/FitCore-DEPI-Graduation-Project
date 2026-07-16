using FitCore.Shared.Enums;
using System.Collections.Generic;

namespace FitCore.Shared.DTOs.Classes
{
    public class ClassDto
    {
        public int ClassID { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int NumberOfSessions { get; set; }
        public decimal Price { get; set; }
        public ClassStatus Status { get; set; }

        public int TrainerID { get; set; }
        public string TrainerName { get; set; } = string.Empty;

        public ICollection<ClassScheduleDto> Schedules { get; set; } = new List<ClassScheduleDto>();
    }
}
