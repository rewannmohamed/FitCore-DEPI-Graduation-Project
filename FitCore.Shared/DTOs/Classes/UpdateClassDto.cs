using FitCore.Shared.Enums;

namespace FitCore.Shared.DTOs.Classes
{
    public class UpdateClassDto
    {
        public string ClassName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public decimal Price { get; set; }
        public int NumberOfSessions { get; set; }
        public ClassStatus Status { get; set; }
    }
}
