using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces.Auth;
using FitCore.BLL.Interfaces.Profile;
using FitCore.DAL.Data.Contexts;
using FitCore.Shared.DTOs.User;
using FitCore.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitCore.BLL.Services.Profile
{
    public class ProfileService(
        FitCoreDbContext dbContext,
        ICurrentUserService userService) : IProfileService
    {
        public async Task<UserDto> GetProfile()
        {
            var userId = userService.UserId ?? throw new UnauthorizedAccessException(nameof(userService));

            var user = await dbContext.Users
                .Include(x => x.UserRoles)
                .Include(x => x.Trainer)
                .Include(x => x.MemberProfile)
                .FirstOrDefaultAsync(x => x.UserID == userId);

            if (user == null) throw new KeyNotFoundException("User not found");

            var roles = user.UserRoles.Select(
                x => new UserRoleDto{
                   Role = x.Role,
                }).ToList();

            UserDto userDto = new UserDto()
            {
                FullName = user.FullName,
                Email = user.Email,
                JoinDate = user.JoinDate,
                PhoneNumber = user.PhoneNumber,
                Status = user.Status,
                UserRoles = roles,
            };

            if (user.Trainer != null)
            {
                TrainerDto trainerDto = new TrainerDto()
                {
                    Bio = user.Trainer.Bio,
                    Specialization = user.Trainer.Specialization,
                    WorkingHours = user.Trainer.WorkingHours,
                };
                userDto.TrainerDto = trainerDto;
            }

            if (user.MemberProfile != null)
            {
                MemberDto memberDto = new MemberDto()
                {
                    QRCodeData = user.MemberProfile.QRCodeData,
                };
                userDto.MemberDto = memberDto;
            }

            return userDto;

        }

        public async Task EditProfile(EditUserDto userDto)
        {
            var errors = new List<string>();

            if (userDto == null)
            {
                errors.Add("The submitted profile data is completely empty.");
                throw new ValidationException(errors);
            }

            var userId = userService.UserId ?? throw new UnauthorizedAccessException(nameof(userService));

            var user = await dbContext.Users
                .Include(x => x.UserRoles)
                .Include(x => x.Trainer)
                .Include(x => x.MemberProfile)
                .FirstOrDefaultAsync(x => x.UserID == userId);

            if (user == null) throw new KeyNotFoundException("User not found");

            user.Email = userDto.Email;
            user.PhoneNumber = userDto.PhoneNumber;
            user.FullName = userDto.FullName;

            if (user.Trainer != null)
            {
                if (userDto.TrainerDto == null)
                {
                    errors.Add("Trainer information is required for trainer accounts.");
                }
                else
                {
                    user.Trainer.Specialization = userDto.TrainerDto.Specialization ?? "N/A";
                    user.Trainer.Bio = userDto.TrainerDto.Bio ?? "N/A";
                    user.Trainer.WorkingHours = userDto.TrainerDto.WorkingHours ?? "N/A";
                }
            }

            if (errors.Any())
            {
                throw new ValidationException(errors);
            }

            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync();
        }
    }
}
