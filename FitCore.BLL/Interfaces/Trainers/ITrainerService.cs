using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.Trainers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FitCore.BLL.Interfaces.Trainers
{
    public interface ITrainerService
    {
        Task<TrainerDto> CreateStaffAsync(CreateStaffDto dto);
        Task<PaginationResponseDto<TrainerDto>> GetAllTrainersAsync(int page, int pageSize);
        Task<TrainerDto> GetTrainerByIdAsync(int trainerId);

        Task<ICollection<TrainerWorkingHourDto>> SetWorkingHoursAsync(int trainerId, SetWorkingHoursDto dto);
        Task<ICollection<TrainerWorkingHourDto>> GetWorkingHoursAsync(int trainerId);

        Task<bool> AssignTrainerToClassAsync(int classId, int trainerId);
        Task<PaginationResponseDto<StaffDto>> GetAllStaffAsync(int page, int pageSize);

        Task<PaginationResponseDto<StaffDto>> GetAllReceptionistsAsync(int page, int pageSize);

        Task<bool> DeleteStaffAsync(int userId);
    }
}
