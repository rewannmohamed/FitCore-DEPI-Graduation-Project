using FitCore.Shared.Enums;
using System;

namespace FitCore.Shared.DTOs.Auth
{
    public class RequestRoleChangeDto
    {
        public UserRoles RequestedRole { get; set; }
    }

    public class RoleChangeRequestDto
    {
        public int RoleChangeRequestID { get; set; }
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRoles CurrentRole { get; set; }
        public UserRoles RequestedRole { get; set; }
        public RoleChangeStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RoleChangeResultDto
    {
        public bool IsPendingApproval { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ReviewRoleChangeDto
    {
        public string? Note { get; set; }
    }
}
