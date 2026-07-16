using FitCore.Shared.Enums;

public class ClassBookingDto
{
    public int BookingID { get; set; }
    public int ClassID { get; set; }
    public string ClassName { get; set; } = string.Empty; 
    public BookingStatus Status { get; set; }
    public List<string> ScheduleDetails { get; set; } = new List<string>();
    public string TrainerName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}