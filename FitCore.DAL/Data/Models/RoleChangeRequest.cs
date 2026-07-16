using FitCore.Shared.Enums;
using System;

namespace FitCore.DAL.Data.Models
{
    public class RoleChangeRequest
    {
        public int RoleChangeRequestID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public UserRoles CurrentRole { get; set; }
        public UserRoles RequestedRole { get; set; }

        public RoleChangeStatus Status { get; set; } = RoleChangeStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedByUserID { get; set; }
        public User? ReviewedByUser { get; set; }

        public string? ReviewNote { get; set; }
    }
}