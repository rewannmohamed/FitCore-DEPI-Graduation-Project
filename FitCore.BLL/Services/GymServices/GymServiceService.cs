using FitCore.BLL.DTOs.Booking;
using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces.GymService;
using FitCore.DAL.Data.Contexts;
using FitCore.DAL.Data.Models;
using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.GymService;
using FitCore.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.BLL.Services.GymServices
{
    public class GymServiceService(FitCoreDbContext DbContext) : IGymServiceService
    {
        public async Task<BookingGymServiceDto> AddGymServiceToBookingAsync(int userId, int gymServiceId)
        {
            var member = await DbContext.Set<MemberProfile>().FirstOrDefaultAsync(m => m.UserID == userId);
            if (member == null)
                throw new KeyNotFoundException("Member profile not found.");

            var service = await DbContext.Set<GymService>().FirstOrDefaultAsync(s => s.ServiceID == gymServiceId);
            if (service == null)
                throw new KeyNotFoundException("Gym Service not found.");


            var hasActiveMembership = await DbContext.Set<Membership>().AnyAsync(m =>
                m.MemberProfileId == member.MemberProfileId &&
                m.GymServiceId == gymServiceId &&
                m.Status == MemberShipStatus.Active &&
                m.EndDate >= DateTime.UtcNow);

            if (hasActiveMembership)
                throw new BusinessRuleException("You already have an active membership for this gym service.");

            var alreadyInBooking = await DbContext.Set<Booking>().AnyAsync(b =>
                b.MemberUserId == member.MemberProfileId &&
                b.GymServiceId == gymServiceId &&
                b.Status == BookingStatus.Booked);

            if (alreadyInBooking)
                throw new BusinessRuleException("This gym service is already in your booking list.");

            var booking = new Booking
            {  
                MemberUserId = member.MemberProfileId,
                ClassID = null,
                GymServiceId = gymServiceId,
                Status = BookingStatus.Booked,
                CreatedAt = DateTime.UtcNow
            };

            await DbContext.Set<Booking>().AddAsync(booking);
            await DbContext.SaveChangesAsync();

            return new BookingGymServiceDto
            {
                BookingId = booking.BookingID,
                GymServiceId = booking.GymServiceId ?? 0,
                ServiceName = service.Name,
                Price = service.Price,
                Category = (int)service.Category,
                DurationInDays = service.DurationInDays,
                AllowedSessionsCount = service.AllowedSessionsCount,
                Status = booking.Status.ToString()
            };
        }

        public async Task<GymServiceDto> CreateGymServiceAsync(CreateGymServiceDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Service name is required.");

            if (dto.Price < 0 || dto.DurationInDays <= 0 || dto.AllowedSessionsCount <= 0)
                throw new ValidationException("Price, Duration, and Allowed Sessions must be strictly positive values.");

            var gymService = new GymService
            {
                Name = dto.Name.Trim(),
                Price = dto.Price,
                Category = dto.Category,
                DurationInDays = dto.DurationInDays,
                AllowedSessionsCount = dto.AllowedSessionsCount
            };

            await DbContext.Set<GymService>().AddAsync(gymService);
            await DbContext.SaveChangesAsync();

            return new GymServiceDto
            {
                ServiceID = gymService.ServiceID,
                Name = gymService.Name,
                Price = gymService.Price,
                Category = gymService.Category,
                DurationInDays = gymService.DurationInDays,
                AllowedSessionsCount = gymService.AllowedSessionsCount
            };
        }

        //exchang to get all bookings:
        public async Task<ICollection<BookingGymServiceDto>> GetMemberGymServiceBookingsAsync(int userId)
        {

            var member = await DbContext.Set<MemberProfile>().FirstOrDefaultAsync(m => m.UserID == userId);
            if (member == null)
                return new List<BookingGymServiceDto>();

            var bookings = await DbContext.Set<Booking>()
                .Where(b => b.MemberUserId == member.MemberProfileId && b.GymServiceId != null && !b.IsDeleted)
                .Include(b => b.GymService)
                .OrderByDescending(b => b.BookingID)
                .ToListAsync();

            return bookings.Select(b => new BookingGymServiceDto
            {
                BookingId = b.BookingID,
                GymServiceId = b.GymServiceId ?? 0,
                ServiceName = b.GymService?.Name ?? string.Empty,
                Price = b.GymService?.Price ?? 0,
                Category = b.GymService != null ? (int)b.GymService.Category : 0,
                DurationInDays = b.GymService?.DurationInDays ?? 0,
                AllowedSessionsCount = b.GymService?.AllowedSessionsCount ?? 0,
                Status = b.Status.ToString()
            }).ToList();
        }
        public async Task<GymServiceDto> UpdateGymServiceAsync(int id, UpdateGymServiceDto dto)
        {
            var service = await DbContext.Set<GymService>().FirstOrDefaultAsync(s => s.ServiceID == id);

            if (service == null)
                throw new KeyNotFoundException("Gym service not found.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Service name is required.");

            if (dto.Price < 0 || dto.DurationInDays <= 0 || dto.AllowedSessionsCount <= 0)
                throw new ValidationException("Price, Duration, and Allowed Sessions must be strictly positive values.");

            service.Name = dto.Name.Trim();
            service.Price = dto.Price;
            service.Category = dto.Category;
            service.DurationInDays = dto.DurationInDays;
            service.AllowedSessionsCount = dto.AllowedSessionsCount;

            DbContext.Set<GymService>().Update(service);
            await DbContext.SaveChangesAsync();

            return new GymServiceDto
            {
                ServiceID = service.ServiceID,
                Name = service.Name,
                Price = service.Price,
                Category = service.Category,
                DurationInDays = service.DurationInDays,
                AllowedSessionsCount = service.AllowedSessionsCount
            };
        }

        public async Task DeleteGymServiceAsync(int id)
        {
            var service = await DbContext.Set<GymService>().FirstOrDefaultAsync(s => s.ServiceID == id);

            if (service == null)
                throw new KeyNotFoundException("Gym service not found.");

            service.IsDeleted = true;
            service.DeletedAt = DateTime.UtcNow;

            DbContext.Set<GymService>().Update(service);
            await DbContext.SaveChangesAsync();
        }

        public async Task<PaginationResponseDto<GymServiceDto>> GetGymServicesAsync(int page, int pageSize, string? searchTerm, ServiceCategory? category)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 50) pageSize = 20;

            var query = DbContext.Set<GymService>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(s => s.Name.ToLower().Contains(lowerSearchTerm));
            }

            if (category.HasValue)
            {
                query = query.Where(s => s.Category == category.Value);
            }

            var totalCount = await query.CountAsync();

            var services = await query
                .OrderByDescending(s => s.ServiceID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new GymServiceDto
                {
                    ServiceID = s.ServiceID,
                    Name = s.Name,
                    Price = s.Price,
                    Category = s.Category,
                    DurationInDays = s.DurationInDays,
                    AllowedSessionsCount = s.AllowedSessionsCount
                })
                .ToListAsync();

            return new PaginationResponseDto<GymServiceDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = services
            };
        }

        public async Task CancelGymServiceBookingAsync(int userId, int bookingId)
        {
            
            var member = await DbContext.Set<MemberProfile>().FirstOrDefaultAsync(m => m.UserID == userId);
            if (member == null)
                throw new KeyNotFoundException("Member profile not found.");

            var booking = await DbContext.Set<Booking>()
                .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.MemberUserId == member.MemberProfileId && b.GymServiceId != null);

            if (booking == null)
                throw new KeyNotFoundException("Booking not found or does not belong to the user.");

            if (booking.Status != BookingStatus.Booked)
                throw new BusinessRuleException("Cannot cancel a booking from its current status.");

            booking.IsDeleted = true;
            booking.DeletedAt = DateTime.UtcNow;
            booking.Status = BookingStatus.Cancelled;

            DbContext.Set<Booking>().Update(booking);
            await DbContext.SaveChangesAsync();
        }

        public async Task<List<BookingResponseDto>> GetAllBookingsAsync(int userId)
        {
            int memberId = await DbContext.MemberProfiles.Where(x => x.UserID == userId).Select(x => x.MemberProfileId).FirstOrDefaultAsync();
            var bookings = await DbContext.Bookings
                .Include(b => b.MemberProfile).ThenInclude(mp => mp.User)
                .Include(b => b.Class).ThenInclude(c => c.Trainer).ThenInclude(t => t.User)
                .Include(b => b.GymService)
                .Where(b => !b.IsDeleted && b.MemberUserId == memberId && b.Status == BookingStatus.Booked)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BookingResponseDto
                {
                    BookingID = b.BookingID,
                    MemberName = b.MemberProfile.User.FullName,
                    BookedItemName = b.ClassID != null ? b.Class!.ClassName : b.GymService!.Name,
                    ItemType = b.ClassID != null ? "Class" : "Gym Service",
                    Price = b.ClassID != null ? b.Class!.Price : b.GymService!.Price,
                    TrainerName = b.ClassID != null && b.Class!.Trainer != null
                                  ? b.Class.Trainer.User.FullName
                                  : "N/A",

                    Status = b.Status.ToString(),
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return bookings;
        }
    }
}
