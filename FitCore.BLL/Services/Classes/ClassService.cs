using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces.Classes;
using FitCore.DAL.Data.Contexts;
using FitCore.DAL.Data.Models;
using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.Classes;
using FitCore.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace FitCore.BLL.Services.Classes
{
    public class ClassService(FitCoreDbContext DbContext) : IClassService
    {
        private const int MaxBrowseRangeDays = 90;

        public async Task<ClassDto> CreateClassAsync(CreateClassDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (string.IsNullOrWhiteSpace(dto.ClassName)) throw new ValidationException("Class name is required.");
            if (dto.Capacity <= 0) throw new ValidationException("Capacity must be greater than zero.");
            if (dto.NumberOfSessions <= 0) throw new ValidationException("Number of sessions must be greater than zero.");
            if (dto.Price <= 0) throw new ValidationException("Price must be greater than zero.");
            if (dto.Schedules == null || !dto.Schedules.Any()) throw new ValidationException("At least one schedule is required.");
            foreach (var slot in dto.Schedules)
            {
                if (slot.EndTime <= slot.StartTime) throw new ValidationException("End time must be after start time.");
            }

            var trainer = await DbContext.Set<Trainer>().FirstOrDefaultAsync(t => t.TrainerID == dto.TrainerID);
            if (trainer == null) throw new KeyNotFoundException("No trainer found with this id.");
            
            var gymClass = new Class
            {
                ClassName = dto.ClassName,
                Description = dto.Description,
                NumberOfSessions = dto.NumberOfSessions,
                TrainerID = dto.TrainerID,
                Status = ClassStatus.Active,
                Capacity = dto.Capacity,
                Price = dto.Price,
                Schedules = dto.Schedules.Select(s => new ClassSchedule { Day = s.Day, StartTime = s.StartTime, EndTime = s.EndTime }).ToList()
            };

            await DbContext.Set<Class>().AddAsync(gymClass);
            await DbContext.SaveChangesAsync();

            return MapToDto(gymClass, trainer);
        }

        public async Task<ClassDto> UpdateClassAsync(int classId, UpdateClassDto dto)
        {
            var gymClass = await DbContext.Set<Class>()
                .Include(c => c.Trainer)
                .Include(c => c.Schedules)
                .FirstOrDefaultAsync(c => c.ClassID == classId);

            if (gymClass == null)
            {
                throw new KeyNotFoundException("No class found with this id.");
            }

            if (dto.Capacity <= 0)
            {
                throw new ValidationException("Capacity must be greater than zero.");
            }

            if (dto.NumberOfSessions <= 0)
            {
                throw new ValidationException("Number of sessions must be greater than zero.");
            }
            if (dto.Price <= 0)
            {
                throw new ValidationException("Price must be greater than zero.");
            }
            Console.WriteLine("________________________________________________________________________");
            Console.WriteLine(dto.Price);
            Console.WriteLine("________________________________________________________________________");
            gymClass.ClassName = dto.ClassName;
            gymClass.Description = dto.Description;
            gymClass.Capacity = dto.Capacity;
            gymClass.Status = dto.Status;
            gymClass.NumberOfSessions = dto.NumberOfSessions;
            gymClass.Price = dto.Price;
           
            DbContext.Set<Class>().Update(gymClass);
            await DbContext.SaveChangesAsync();
            Console.WriteLine(gymClass);
            return MapToDto(gymClass, gymClass.Trainer);
        }

        public async Task<bool> DeleteClassAsync(int classId)
        {
            var gymClass = await DbContext.Set<Class>()
                .Include(c => c.Schedules)
                .FirstOrDefaultAsync(c => c.ClassID == classId);

            if (gymClass == null)
            {
                throw new KeyNotFoundException("No class found with this id.");
            }

            var hasActiveMemberships = await DbContext.Set<Membership>().AnyAsync(m =>
                m.ClassID == classId &&
                (m.Status == MemberShipStatus.Active || m.Status == MemberShipStatus.Freezed));

            if (hasActiveMemberships)
            {
                throw new BusinessRuleException("This class has active or frozen memberships and cannot be deleted. Deactivate it instead.");
            }

            var hasPendingBookings = await DbContext.Set<Booking>().AnyAsync(b =>
                b.ClassID == classId && b.Status == BookingStatus.Booked);

            if (hasPendingBookings)
            {
                throw new BusinessRuleException("This class has pending bookings and cannot be deleted. Cancel them first, or deactivate the class instead.");
            }

            DbContext.Set<ClassSchedule>().RemoveRange(gymClass.Schedules);
            DbContext.Set<Class>().Remove(gymClass);
            var affected = await DbContext.SaveChangesAsync();

            return affected > 0;
        }

        public async Task<PaginationResponseDto<ClassDto>> GetAllClassesAsync(int page, int pageSize)
        {
            if (page <= 0) page = 1;
            const int maxPageSize = 50;
            if (pageSize <= 0 || pageSize > maxPageSize) pageSize = 20;

            var query = DbContext.Set<Class>()
                .Include(c => c.Trainer).ThenInclude(t => t.User)
                .Include(c => c.Schedules)
                .OrderBy(c => c.ClassID);

            var totalCount = await query.CountAsync();

            var classes = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginationResponseDto<ClassDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = classes.Select(c => MapToDto(c, c.Trainer)).ToList()
            };
        }

        public async Task<ClassDto> GetClassByIdAsync(int classId)
        {
            var gymClass = await DbContext.Set<Class>()
                .Include(c => c.Trainer).ThenInclude(t => t.User)
                .Include(c => c.Schedules)
                .FirstOrDefaultAsync(c => c.ClassID == classId);

            if (gymClass == null) throw new KeyNotFoundException("No class found with this id.");
            return MapToDto(gymClass, gymClass.Trainer);
        }

        public async Task<ClassScheduleDto> AddScheduleAsync(int classId, ClassScheduleDto dto)
        {
            var gymClass = await DbContext.Set<Class>().FirstOrDefaultAsync(c => c.ClassID == classId);
            if (gymClass == null)
            {
                throw new KeyNotFoundException("No class found with this id.");
            }

            if (dto.EndTime <= dto.StartTime)
            {
                throw new ValidationException("Schedule end time must be after start time.");
            }

            var schedule = new ClassSchedule
            {
                ClassID = classId,
                Day = dto.Day,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
            };

            await DbContext.Set<ClassSchedule>().AddAsync(schedule);
            await DbContext.SaveChangesAsync();

            return new ClassScheduleDto
            {
                Id = schedule.Id,
                Day = schedule.Day,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
            };
        }

        public async Task<PaginationResponseDto<ClassWithSchedulesDto>> BrowseClassesAsync(
            DateTime fromDate,
            DateTime toDate,
            int page,
            int pageSize
        )
        {
            if (page <= 0) page = 1;

            const int maxPageSize = 50;
            if (pageSize <= 0 || pageSize > maxPageSize)
                pageSize = 20;

            fromDate = fromDate.Date;
            toDate = toDate.Date;

            if (toDate < fromDate)
                throw new ValidationException("The end date must be on or after the start date.");


            var query = DbContext.Set<Class>()
                .Where(c => c.Status == ClassStatus.Active);

            var totalCount = await query.CountAsync();

            var classes = await query
                .Include(c => c.Trainer)
                    .ThenInclude(t => t.User)
                .Include(c => c.Schedules)
                .OrderBy(c => c.ClassName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!classes.Any())
            {
                return new PaginationResponseDto<ClassWithSchedulesDto>
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    Data = new List<ClassWithSchedulesDto>()
                };
            }

            var classIds = classes.Select(c => c.ClassID).ToList();

            var activeMembershipsCount = await DbContext.Set<Membership>()
                .Where(m => m.ClassID != null && classIds.Contains(m.ClassID.Value)
                         && (m.Status == MemberShipStatus.Active || m.Status == MemberShipStatus.Freezed)
                         && m.EndDate >= DateTime.UtcNow)
                .GroupBy(m => m.ClassID)
                .Select(g => new { ClassID = g.Key!.Value, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassID, x => x.Count);

            var pendingBookingsCount = await DbContext.Bookings
                .Where(b => b.ClassID != null && classIds.Contains(b.ClassID.Value)
                         && b.Status == BookingStatus.Booked)
                .GroupBy(b => b.ClassID)
                .Select(g => new
                {
                    ClassID = g.Key!.Value,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.ClassID, x => x.Count);


            var resultData = classes.Select(c => new ClassWithSchedulesDto
            {
                ClassID = c.ClassID,
                ClassName = c.ClassName,
                Description = c.Description,
                TrainerName = c.Trainer?.User?.FullName ?? "No Trainer Assigned",
                Capacity = c.Capacity,
                BookedCount = pendingBookingsCount.GetValueOrDefault(c.ClassID, 0),
                Price = c.Price,

                Schedules = c.Schedules.Select(s => new IndividualScheduleDto
                {
                    ClassScheduleID = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    DayName = s.Day.ToString(), 


                    CalculatedDate = fromDate.AddDays(((int)s.Day - (int)fromDate.DayOfWeek + 7) % 7)
                })
                .OrderBy(s => s.StartTime) 
                .ToList()
            }).ToList();

            return new PaginationResponseDto<ClassWithSchedulesDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = resultData
            };
        }

        public async Task<ClassBookingDto> BookClassAsync(int memberUserId, int classId)
        {

            var member = await DbContext.Set<User>()
                .Include(u => u.MemberProfile)
                .FirstOrDefaultAsync(m => m.UserID == memberUserId);

            if (member == null)
                throw new KeyNotFoundException("User not found.");

            if (member.MemberProfile == null)
                throw new KeyNotFoundException("Member profile not found for this user.");

            var gymClass = await DbContext.Set<Class>()
                .Include(c => c.Schedules)
                .FirstOrDefaultAsync(c => c.ClassID == classId);

            if (gymClass == null)
                throw new KeyNotFoundException("Class not found.");

            if (gymClass.Status != ClassStatus.Active)
                throw new BusinessRuleException("This class is not currently active.");

            var activeMembersCount = await DbContext.Set<Membership>()
                .CountAsync(m => m.ClassID == classId &&
                        (m.Status == MemberShipStatus.Active || m.Status == MemberShipStatus.Freezed));

            var pendingBookingsCount = await DbContext.Set<Booking>()
                .CountAsync(b => b.ClassID == classId && b.Status == BookingStatus.Booked);

            if (activeMembersCount + pendingBookingsCount >= gymClass.Capacity)
            {
                throw new BusinessRuleException("This class has reached its maximum capacity and is fully booked.");
            }


            var hasActiveMembership = await DbContext.Set<Membership>().AnyAsync(m =>
                m.MemberProfileId == member.MemberProfile.MemberProfileId &&
                m.ClassID == classId &&
                (m.Status == MemberShipStatus.Active || m.Status == MemberShipStatus.Freezed) &&
                m.EndDate >= DateTime.UtcNow);

            if (hasActiveMembership)
                throw new BusinessRuleException("You already have an active membership for this class.");

            var alreadyInBooking = await DbContext.Bookings.AnyAsync(b =>
                b.MemberUserId == member.MemberProfile.MemberProfileId &&
                b.ClassID == classId &&
                b.Status == BookingStatus.Booked);

            if (alreadyInBooking)
                throw new BusinessRuleException("This class is already in your booking list.");

            var booking = new Booking
            {
                MemberUserId = member.MemberProfile.MemberProfileId,
                ClassID = classId,
                GymServiceId = null,
                Status = BookingStatus.Booked,
                CreatedAt = DateTime.UtcNow
            };

            await DbContext.Bookings.AddAsync(booking);
            await DbContext.SaveChangesAsync();

            return new ClassBookingDto
            {
                BookingID = booking.BookingID,
                ClassID = classId,
                ClassName = gymClass.ClassName,
                Status = booking.Status,
                ScheduleDetails = gymClass.Schedules.Select(s => $"{s.Day}: {s.StartTime} - {s.EndTime}").ToList()
            };
        }
        public async Task<bool> CancelBookingAsync(int memberUserId, int bookingId)
        {
            var member = await DbContext.Set<User>()
               .Include(u => u.MemberProfile)
               .FirstOrDefaultAsync(m => m.UserID == memberUserId);

            if (member == null)
                throw new KeyNotFoundException("User not found.");

            if (member.MemberProfile == null)
                throw new KeyNotFoundException("Member profile not found for this user.");

            var booking = await DbContext.Set<Booking>().FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null || booking.MemberUserId != member.MemberProfile.MemberProfileId)
            {
                throw new KeyNotFoundException("No booking found with this id for this member.");
            }

            if (booking.Status != BookingStatus.Booked)
            {
                throw new BusinessRuleException("This booking cannot be cancelled.");
            }

            booking.Status = BookingStatus.Cancelled;
            DbContext.Bookings.Update(booking);
            var affected = await DbContext.SaveChangesAsync();

            return affected > 0;
        }

        public async Task<ICollection<ClassBookingDto>> GetMemberBookingsAsync(int memberUserId)
        {
            var bookings = await DbContext.Set<Booking>()
                .Include(b => b.Class)
                    .ThenInclude(c => c!.Schedules)
                .Include(b => b.Class)
                    .ThenInclude(c => c!.Trainer)
                        .ThenInclude(t => t!.User)
                .Include(b => b.MemberProfile)
                .Where(b => b.MemberProfile.UserID == memberUserId && b.ClassID != null)
                .ToListAsync();

            return bookings.Select(b => new ClassBookingDto
            {
                BookingID = b.BookingID,
                ClassID = b.ClassID ?? 0,
                ClassName = b.Class!.ClassName, 
                Status = b.Status,
                TrainerName = b.Class.Trainer?.User?.FullName ?? "No Trainer",
                Price = b.Class.Price, 
                ScheduleDetails = b.Class.Schedules
                    .Select(s => $"{s.Day}: {s.StartTime} - {s.EndTime}")
                    .ToList()
            }).ToList();
        }
        private static ClassDto MapToDto(Class gymClass, Trainer? trainer)
        {
            return new ClassDto
            {
                ClassID = gymClass.ClassID,
                ClassName = gymClass.ClassName,
                Description = gymClass.Description,
                Status = gymClass.Status,
                TrainerID = gymClass.TrainerID,
                TrainerName = trainer?.User?.FullName ?? string.Empty,
                Capacity = gymClass.Capacity,
                Schedules = gymClass.Schedules?.Select(s => new ClassScheduleDto
                {
                    Id = s.Id,
                    Day = s.Day,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                }).ToList() ?? new List<ClassScheduleDto>(),
                Price = gymClass.Price,
                NumberOfSessions = gymClass.NumberOfSessions
            };
        }
    }
}