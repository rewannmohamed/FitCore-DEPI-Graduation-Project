using FitCore.BLL.DTOs.Booking;
using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.GymService;
using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.BLL.Interfaces.GymService
{
    public interface IGymServiceService
    {
        public Task<BookingGymServiceDto> AddGymServiceToBookingAsync(int memberUserId, int gymServiceId);
        public Task<GymServiceDto> CreateGymServiceAsync(CreateGymServiceDto dto);
        public Task<GymServiceDto> UpdateGymServiceAsync(int id, UpdateGymServiceDto dto);
        public Task DeleteGymServiceAsync(int id);
        public Task<PaginationResponseDto<GymServiceDto>> GetGymServicesAsync(int page, int pageSize, string? searchTerm, ServiceCategory? category);
        public Task CancelGymServiceBookingAsync(int userId, int bookingId);
        //public Task RemoveBookingsAfterCheckoutAsync(int memberUserId, List<int> bookingIds);
        public Task<ICollection<BookingGymServiceDto>> GetMemberGymServiceBookingsAsync(int memberUserId);
        Task<List<BookingResponseDto>> GetAllBookingsAsync(int userId);
    }
}
