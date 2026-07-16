using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.MemberDashboard
{
    public class DigitalPassDto
    {
        public string MemberName { get; set; } = string.Empty;
        public string MembershipType { get; set; } = string.Empty;
        public DateTime ValidUntil { get; set; }
        public string QrCodeData { get; set; } = string.Empty;
    }
}
