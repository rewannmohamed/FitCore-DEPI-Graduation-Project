using System;

namespace FitCore.BLL.DTOs.Booking
{
    public class BookingResponseDto
    {
        public int BookingID { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string BookedItemName { get; set; } = string.Empty;

        public string ItemType { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string? TrainerName { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}