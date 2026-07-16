using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.Shared.DTOs.User
{
    public class UserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
        public DateTime JoinDate { get; set; }
        public ICollection<UserRoleDto> UserRoles { get; set; } = new List<UserRoleDto>();
        public TrainerDto? TrainerDto { get; set; }
        public MemberDto? MemberDto { get; set; }
    }

    public class MemberDto
    {
        public string QRCodeData { get; set; } = string.Empty;
    }

    public class TrainerDto
    {
        public string Specialization { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string WorkingHours { get; set; } = string.Empty;
    }

    public class UserRoleDto
    {
        public UserRoles Role { get; set; }
    }

    public class EditUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public TrainerDto? TrainerDto { get; set; }

    }
}
