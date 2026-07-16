using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.Attendance
{
    public class CheckInRequestDto
    {
        public string QrCode { get; set; } = string.Empty;
        public int? ClassScheduleId { get; set; }
    }
}
