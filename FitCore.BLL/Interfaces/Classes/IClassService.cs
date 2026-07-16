using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.Classes;

namespace FitCore.BLL.Interfaces.Classes
{
    public interface IClassService
    {
        Task<ClassDto> CreateClassAsync(CreateClassDto dto);
        Task<ClassDto> UpdateClassAsync(int classId, UpdateClassDto dto);
        Task<PaginationResponseDto<ClassDto>> GetAllClassesAsync(int page, int pageSize);
        Task<ClassDto> GetClassByIdAsync(int classId);
        Task<ClassScheduleDto> AddScheduleAsync(int classId, ClassScheduleDto dto);

        // Members browsing available class occurrences within a date range
        Task<PaginationResponseDto<ClassWithSchedulesDto>> BrowseClassesAsync(
             DateTime fromDate,
             DateTime toDate,
             int page,
             int pageSize
         );
        Task<bool> DeleteClassAsync(int classId);
        Task<ClassBookingDto> BookClassAsync(int memberUserId, int classId);
        Task<bool> CancelBookingAsync(int memberUserId, int bookingId);
        Task<ICollection<ClassBookingDto>> GetMemberBookingsAsync(int memberUserId);
    }
}
