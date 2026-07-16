public class ClassWithSchedulesDto
{
    public int ClassID { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TrainerName { get; set; } = "No Trainer Assigned";
    public int Capacity { get; set; }
    public int BookedCount { get; set; }
    public decimal Price { get; set; }
    // 💡 مصفوفة المواعيد الخاصة بهذا الكلاس
    public List<IndividualScheduleDto> Schedules { get; set; } = new();
}

public class IndividualScheduleDto
{
    public int ClassScheduleID { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string DayName { get; set; } = string.Empty; 
    public DateTime CalculatedDate { get; set; } 
}