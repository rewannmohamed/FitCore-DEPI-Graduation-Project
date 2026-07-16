using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.MemberShip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.BLL.Interfaces.Membership
{
    public interface IMembershipService
    {
        public Task<ICollection<MembershipDto>> GetMemberMembershipsAsync(int memberProfileId);
        public Task<MembershipDto> GetMembershipByIdAsync(int membershipId);
        public Task<bool> FreezeMembershipAsync(int membershipId, int freezeDays);
        public Task<PaginationResponseDto<AdminMembershipDto>> GetAllActiveMembershipsForAdminAsync(int page, int pageSize);
        public Task GenerateMembershipsFromInvoiceAsync(int invoiceId);
        public Task<bool> UnfreezeMembershipAsync(int membershipId);
        public Task AutoUnfreezeExpiredFreezesAsync();

    }
}
