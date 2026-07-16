using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.Attendance
{
    public class CheckInResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string MembershipType { get; set; } = string.Empty;
        public int RemainingSessions { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime CheckInTime { get; set; }
    }
}
