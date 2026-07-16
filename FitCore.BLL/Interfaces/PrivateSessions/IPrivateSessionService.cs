using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.PrivateSessions;
using FitCore.Shared.Enums;

namespace FitCore.BLL.Interfaces.PrivateSessions
{
    public interface IPrivateSessionService
    {
        Task<PrivateSessionDto> CreatePrivateSessionAsync(CreatePrivateSessionDto dto);
        Task<PrivateSessionDto> AssignTrainerAsync(int privateSessionId, int trainerId);
        Task<ICollection<PrivateSessionDto>> GetSessionsByTrainerAsync(int trainerId);
        Task<ICollection<PrivateSessionDto>> GetSessionsByMemberAsync(int memberUserId);
        Task<bool> CancelSessionAsync(int privateSessionId);
        Task<bool> CompleteSessionAsync(int privateSessionId);
        Task<PaginationResponseDto<PrivateSessionDto>> GetAllSessionsAsync(int page, int pageSize, PrivateSessionStatus? status);
    }
}
