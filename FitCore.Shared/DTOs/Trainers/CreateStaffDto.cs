using FitCore.Shared.Enums;
using System;

namespace FitCore.Shared.DTOs.Trainers
{
    public class CreateStaffDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Only UserRoles.Trainer or UserRoles.Receptionist are accepted here.
        public UserRoles Role { get; set; }

        // Only relevant when Role == Trainer
        public string Specialization { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
    }
}
