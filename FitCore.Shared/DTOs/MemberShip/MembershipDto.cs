using FitCore.Shared.Enums;


namespace FitCore.Shared.DTOs.MemberShip
{
    public class CreateMembershipDto
    {
        //public int UserId { get; set; }
        public int MemberProfileId { get; set; }

        public int? GymServiceId { get; set; }
        public int? ClassId { get; set; }
        public int InvoiceId { get; set; }

        public bool IsAutoRenew { get; set; } = false;
    }

    public class MembershipDto
    {
        public int MembershipID { get; set; }
        public string MembershipType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public MemberShipStatus Status { get; set; }
        public int? RemainingSessions { get; set; }
    }

    public class AdminMembershipDto
    {
        public int MembershipID { get; set; }
        public int MemberProfileId { get; set; }
        public string MemberName { get; set; } = string.Empty; // اسم العميل

        public string MembershipType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public MemberShipStatus Status { get; set; }
        public int? RemainingSessions { get; set; }
    }
}
