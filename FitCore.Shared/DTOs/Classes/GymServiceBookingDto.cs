using FitCore.Shared.Enums;

public class GymServiceBookingDto
{
    public int BookingID { get; set; }
    public int GymServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public BookingStatus Status { get; set; }

    public decimal Price { get; set; }
    public int DurationInDays { get; set; }
    public int AllowedSessionsCount { get; set; }
    public int Category { get; set; }
}