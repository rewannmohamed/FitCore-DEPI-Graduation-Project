namespace FitCore.Shared.DTOs.GymService
{
    public class BookingGymServiceDto
    {
        public int BookingId { get; set; }
        public int GymServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Category { get; set; } 
        public int DurationInDays { get; set; }
        public int AllowedSessionsCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}