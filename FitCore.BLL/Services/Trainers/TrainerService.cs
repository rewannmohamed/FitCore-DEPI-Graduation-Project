using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces.Trainers;
using FitCore.DAL.Data.Contexts;
using FitCore.DAL.Data.Models;
using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.Trainers;
using FitCore.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FitCore.BLL.Services.Trainers
{
    public class TrainerService(
        FitCoreDbContext DbContext,
        IPasswordHasher<User> _passwordHasher) : ITrainerService
    {
        public async Task<TrainerDto> CreateStaffAsync(CreateStaffDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.Role != UserRoles.Trainer && dto.Role != UserRoles.Receptionist)
                throw new BusinessRuleException("Only Trainer or Receptionist roles are allowed.");

            if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email))
                throw new ValidationException("Full name and email are required.");

            if (await DbContext.Users.AnyAsync(u => u.Email == dto.Email))
                throw new BusinessRuleException("Email already exists.");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Status = UserStatus.Active,
                JoinDate = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            user.UserRoles.Add(new UserRole { Role = dto.Role });

            if (dto.Role == UserRoles.Trainer)
            {
                user.Trainer = new Trainer { Specialization = dto.Specialization, Bio = dto.Bio };
            }

            await DbContext.Users.AddAsync(user);
            await DbContext.SaveChangesAsync();

            return dto.Role == UserRoles.Trainer
                ? MapToDto(user.Trainer!, user)
                : new TrainerDto { UserID = user.UserID, FullName = user.FullName, Email = user.Email };
        }

        public async Task<PaginationResponseDto<TrainerDto>> GetAllTrainersAsync(int page, int pageSize)
        {
            if (page <= 0) page = 1;
            const int maxPageSize = 50;
            if (pageSize <= 0 || pageSize > maxPageSize) pageSize = 20;

            var query = DbContext.Set<Trainer>()
                .Include(t => t.User)
                .Include(t => t.WorkingHoursSchedule)
                .OrderBy(t => t.TrainerID);

            var totalCount = await query.CountAsync();

            var trainers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginationResponseDto<TrainerDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = trainers.Select(t => MapToDto(t, t.User)).ToList()
            };
        }

        public async Task<TrainerDto> GetTrainerByIdAsync(int trainerId)
        {
            var trainer = await DbContext.Set<Trainer>()
                .Include(t => t.User)
                .Include(t => t.WorkingHoursSchedule)
                .FirstOrDefaultAsync(t => t.UserID == trainerId);

            if (trainer == null)
            {
                throw new KeyNotFoundException("No trainer found with this id.");
            }

            return MapToDto(trainer, trainer.User);
        }

        public async Task<ICollection<TrainerWorkingHourDto>> SetWorkingHoursAsync(int trainerId, SetWorkingHoursDto dto)
        {
            var trainer = await DbContext.Set<Trainer>()
                .Include(t => t.WorkingHoursSchedule)
                .FirstOrDefaultAsync(t => t.TrainerID == trainerId);

            if (trainer == null)
            {
                throw new KeyNotFoundException("No trainer found with this id.");
            }

            foreach (var slot in dto.WorkingHours)
            {
                if (slot.EndTime <= slot.StartTime)
                {
                    throw new ValidationException("Working hour end time must be after start time.");
                }
            }

            DbContext.Set<TrainerWorkingHour>().RemoveRange(trainer.WorkingHoursSchedule);

            var newSlots = dto.WorkingHours.Select(w => new TrainerWorkingHour
            {
                TrainerID = trainerId,
                Day = w.Day,
                StartTime = w.StartTime,
                EndTime = w.EndTime,
            }).ToList();

            await DbContext.Set<TrainerWorkingHour>().AddRangeAsync(newSlots);
            await DbContext.SaveChangesAsync();

            return newSlots.Select(w => new TrainerWorkingHourDto
            {
                Id = w.Id,
                Day = w.Day,
                StartTime = w.StartTime,
                EndTime = w.EndTime,
            }).ToList();
        }

        public async Task<ICollection<TrainerWorkingHourDto>> GetWorkingHoursAsync(int trainerId)
        {

            var trainer = await DbContext.Set<Trainer>()
                .Include(t => t.WorkingHoursSchedule)
                .FirstOrDefaultAsync(t => t.TrainerID == trainerId);

            if (trainer == null)
            {
                throw new KeyNotFoundException("No trainer found with this id.");
            }

            return await DbContext.Set<TrainerWorkingHour>()
                .Where(w => w.TrainerID == trainer.TrainerID)
                .Select(w => new TrainerWorkingHourDto
                {
                    Id = w.Id,
                    Day = w.Day,
                    StartTime = w.StartTime,
                    EndTime = w.EndTime,
                })
                .ToListAsync();
        }

        public async Task<bool> AssignTrainerToClassAsync(int classId, int trainerId)
        {
            Console.WriteLine("________________________________________________________________________________");
            Console.WriteLine(trainerId);
            Console.WriteLine("________________________________________________________________________________");
            var gymClass = await DbContext.Set<Class>().FirstOrDefaultAsync(c => c.ClassID == classId);
            if (gymClass == null)
            {
                throw new KeyNotFoundException("No class found with this id.");
            }

            var trainer = await DbContext.Set<Trainer>()
                .Include(t => t.WorkingHoursSchedule)
                .FirstOrDefaultAsync(t => t.TrainerID == trainerId);

            if (trainer == null)
            {
                throw new KeyNotFoundException("No trainer found with this id.");
            }

            gymClass.TrainerID = trainerId;
            DbContext.Set<Class>().Update(gymClass);
            var affected = await DbContext.SaveChangesAsync();

            return affected > 0;
        }

        public async Task<PaginationResponseDto<StaffDto>> GetAllStaffAsync(int page, int pageSize)
        {
            return await GetStaffByRolesAsync(new[] { UserRoles.Trainer, UserRoles.Receptionist }, page, pageSize);
        }

        public async Task<PaginationResponseDto<StaffDto>> GetAllReceptionistsAsync(int page, int pageSize)
        {
            return await GetStaffByRolesAsync(new[] { UserRoles.Receptionist }, page, pageSize);
        }

        private async Task<PaginationResponseDto<StaffDto>> GetStaffByRolesAsync(UserRoles[] roles, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            const int maxPageSize = 50;
            if (pageSize <= 0 || pageSize > maxPageSize) pageSize = 20;

            var query = DbContext.Set<User>()
                .Include(u => u.UserRoles)
                .Include(u => u.Trainer).ThenInclude(t => t!.WorkingHoursSchedule)
                .Where(u => !u.IsDeleted && u.UserRoles.Any(r => roles.Contains(r.Role)))
                .OrderBy(u => u.FullName);

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = users.Select(u =>
            {
                var role = u.UserRoles.First(r => roles.Contains(r.Role)).Role;

                return new StaffDto
                {
                    UserID = u.UserID,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = role,
                    TrainerID = u.Trainer?.TrainerID,
                    Specialization = u.Trainer?.Specialization,
                    Bio = u.Trainer?.Bio,
                    WorkingHours = u.Trainer?.WorkingHoursSchedule?.Select(w => new TrainerWorkingHourDto
                    {
                        Id = w.Id,
                        Day = w.Day,
                        StartTime = w.StartTime,
                        EndTime = w.EndTime,
                    }).ToList() ?? new List<TrainerWorkingHourDto>()
                };
            }).ToList();

            return new PaginationResponseDto<StaffDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = data
            };
        }

        public async Task<bool> DeleteStaffAsync(int userId)
        {
            var user = await DbContext.Set<User>()
                .Include(u => u.UserRoles)
                .Include(u => u.Trainer)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                throw new KeyNotFoundException("No staff member found with this id.");

            var isTrainer = user.UserRoles.Any(r => r.Role == UserRoles.Trainer);
            var isReceptionist = user.UserRoles.Any(r => r.Role == UserRoles.Receptionist);

            if (!isTrainer && !isReceptionist)
                throw new BusinessRuleException("This user is not a Trainer or Receptionist - use the correct endpoint to delete other user types.");

            if (isTrainer && user.Trainer != null)
            {
                var hasActiveClasses = await DbContext.Set<Class>().AnyAsync(c =>
                    c.TrainerID == user.Trainer.TrainerID && c.Status == ClassStatus.Active);

                if (hasActiveClasses)
                    throw new BusinessRuleException("This trainer is still assigned to one or more active classes. Reassign those classes to another trainer before deleting.");
            }

            // Soft delete, consistent with ISoftDelete used elsewhere in this app - a hard delete here
            // would risk breaking FK references from AuditLogs, Payments, Bookings, etc. that point
            // at this User.
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;

            DbContext.Set<User>().Update(user);
            var affected = await DbContext.SaveChangesAsync();

            return affected > 0;
        }

        private static TrainerDto MapToDto(Trainer trainer, User user)
        {
            return new TrainerDto
            {
                TrainerID = trainer.TrainerID,
                UserID = trainer.UserID,
                FullName = user?.FullName ?? string.Empty,
                Email = user?.Email ?? string.Empty,
                PhoneNumber = user?.PhoneNumber ?? string.Empty,
                Specialization = trainer.Specialization,
                Bio = trainer.Bio,
                WorkingHours = trainer.WorkingHoursSchedule?.Select(w => new TrainerWorkingHourDto
                {
                    Id = w.Id,
                    Day = w.Day,
                    StartTime = w.StartTime,
                    EndTime = w.EndTime,
                }).ToList() ?? new List<TrainerWorkingHourDto>()
            };
        }
    }
}
