using FitCore.Shared.DTOs.GymService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.Classes
{
    public class UserBookingsDto
    {
        public ICollection<ClassBookingDto> ClassBookings { get; set; } = new List<ClassBookingDto>();
        public ICollection<BookingGymServiceDto> GymServiceBookings { get; set; } = new List<BookingGymServiceDto>();
    }
}
