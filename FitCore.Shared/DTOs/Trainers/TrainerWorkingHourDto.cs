using System;

namespace FitCore.Shared.DTOs.Trainers
{
    public class TrainerWorkingHourDto
    {
        public int Id { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
