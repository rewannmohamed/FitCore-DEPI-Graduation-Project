using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.GymService
{
    public class GymServiceDto
    {
        public int ServiceID { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public ServiceCategory Category { get; set; }
        public int DurationInDays { get; set; }
        public int AllowedSessionsCount { get; set; }
    }

    public class CreateGymServiceDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public ServiceCategory Category { get; set; }
        public int DurationInDays { get; set; }
        public int AllowedSessionsCount { get; set; }
    }

    public class UpdateGymServiceDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public ServiceCategory Category { get; set; }
        public int DurationInDays { get; set; }
        public int AllowedSessionsCount { get; set; }
    }
}
