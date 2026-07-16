using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces.Auth;
using FitCore.DAL.Data.Contexts;
using FitCore.DAL.Data.Models;
using FitCore.Shared.DTOs.Auth;
using FitCore.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitCore.BLL.Services.Auth
{
    public class AuthService(
        FitCoreDbContext dbContext,
        IJwtTokenGenerator _jwtTokenGenerator,
        IPasswordHasher<User> _passwordHasher) : IAuthService
    {

        private const string ManageUsersActionUrl = "/html/Auth/manage-users.html";
        public async Task<AuthResponseDto> Login(LoginDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                throw new ValidationException(new List<string> { "Email and password are required." });
            }

            var user = await dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email && !u.IsDeleted);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Not user found.");
            }

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            if (user.Status == UserStatus.Blocked || user.Status == UserStatus.Suspended)
            {
                throw new BusinessRuleException("This account is blocked/suspended. Please contact the gym administration.");
            }

            return BuildAuthResponse(user);
        }

        public async Task<AuthResponseDto> RegisterMember(RegisterMemberDto dto)
        {
            var errors = ValidateBasicInfo(dto.FullName, dto.Email, dto.PhoneNumber, dto.Password);
            if (errors.Any())
            {
                throw new ValidationException(errors);
            }

            if (await dbContext.Users.AnyAsync(u => u.Email == dto.Email && !u.IsDeleted))
            {
                throw new BusinessRuleException("An account with this email already exists.");
            }

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Status = UserStatus.Active,
                JoinDate = DateTime.UtcNow,
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            user.UserRoles.Add(new UserRole { Role = UserRoles.Member });
            user.MemberProfile = new MemberProfile { QRCodeData = Guid.NewGuid().ToString("N") };
            user.Cart = new Cart();

            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            return BuildAuthResponse(user);
        }

        public async Task<AuthResponseDto> CreateStaff(CreateStaffDto dto)
        {
            //if (dto.Role != UserRoles.Trainer && dto.Role != UserRoles.Receptionist)
            //{
            //    throw new BusinessRuleException("Staff accounts can only be created with the Trainer or Receptionist role.");
            //}

            var errors = ValidateBasicInfo(dto.FullName, dto.Email, dto.PhoneNumber, dto.Password);
            if (errors.Any())
            {
                throw new ValidationException(errors);
            }

            if (await dbContext.Users.AnyAsync(u => u.Email == dto.Email && !u.IsDeleted))
            {
                throw new BusinessRuleException("An account with this email already exists.");
            }

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Status = UserStatus.Active,
                JoinDate = DateTime.UtcNow,
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            user.UserRoles.Add(new UserRole { Role = dto.Role });

            if (dto.Role == UserRoles.Trainer)
            {
                user.Trainer = new Trainer
                {

                    Specialization = "N/A",
                    Bio = "N/A",
                    WorkingHours = "N/A",
                };
            }

            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            return BuildAuthResponse(user);
        }

        public async Task PromoteMemberToTrainer(int userId)
        {
            var user = await dbContext.Users
                .Include(u => u.UserRoles)
                .Include(u => u.Trainer)
                .FirstOrDefaultAsync(u => u.UserID == userId && !u.IsDeleted);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            bool isMember = user.UserRoles.Any(r => r.Role == UserRoles.Member && !r.IsDeleted);
            if (!isMember)
            {
                throw new BusinessRuleException("Only Member accounts can be promoted to Trainer.");
            }

            bool isAlreadyTrainer = user.UserRoles.Any(r => r.Role == UserRoles.Trainer && !r.IsDeleted);
            if (isAlreadyTrainer)
            {
                throw new BusinessRuleException("This user is already a Trainer.");
            }

            user.UserRoles.Add(new UserRole { Role = UserRoles.Trainer });

            if (user.Trainer == null)
            {
                user.Trainer = new Trainer
                {
                    Specialization = "N/A",
                    Bio = "N/A",
                    WorkingHours = "N/A",
                };
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task<List<ManageUserDto>> GetAllUsers()
        {
            var users = await dbContext.Users
                .Include(u => u.UserRoles)
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.JoinDate)
                .ToListAsync();

            return users.Select(u => new ManageUserDto
            {
                UserID = u.UserID,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Status = u.Status,
                JoinDate = u.JoinDate,
                Roles = u.UserRoles.Where(r => !r.IsDeleted).Select(r => r.Role.ToString()).ToList(),
            }).ToList();
        }

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------
        private static List<string> ValidateBasicInfo(string fullName, string email, string phoneNumber, string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(fullName))
                errors.Add("Full name is required.");

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                errors.Add("A valid email is required.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                errors.Add("Phone number is required.");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                errors.Add("Password must be at least 6 characters.");

            return errors;
        }

        private AuthResponseDto BuildAuthResponse(User user)
        {
            var roleNames = user.UserRoles.Where(r => !r.IsDeleted).Select(r => r.Role.ToString()).ToList();
            var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user, roleNames);

            return new AuthResponseDto
            {
                UserID = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                Roles = roleNames,
                Token = token,
                ExpiresAt = expiresAt,
            };
        }


        // ------------------------------------------------------------
        // Self-service Role Change (Member <-> Trainer)
        // ------------------------------------------------------------

        public async Task<RoleChangeResultDto> RequestRoleChange(int userId, UserRoles requestedRole)
        {
            if (requestedRole != UserRoles.Member && requestedRole != UserRoles.Trainer)
            {
                throw new BusinessRuleException("You can only self-request switching between Member and Trainer.");
            }

            var user = await dbContext.Users
                .Include(u => u.UserRoles)
                .Include(u => u.Trainer)
                .Include(u => u.MemberProfile)
                .FirstOrDefaultAsync(u => u.UserID == userId && !u.IsDeleted);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            bool isMember = user.UserRoles.Any(r => r.Role == UserRoles.Member && !r.IsDeleted);
            bool isTrainer = user.UserRoles.Any(r => r.Role == UserRoles.Trainer && !r.IsDeleted);

            if (requestedRole == UserRoles.Trainer)
            {
                if (!isMember)
                {
                    throw new BusinessRuleException("Only Member accounts can request to become a Trainer.");
                }
                if (isTrainer)
                {
                    throw new BusinessRuleException("This user is already a Trainer.");
                }

                bool alreadyPending = await dbContext.RoleChangeRequests.AnyAsync(r =>
                    r.UserID == userId && r.RequestedRole == UserRoles.Trainer && r.Status == RoleChangeStatus.Pending);
                if (alreadyPending)
                {
                    throw new BusinessRuleException("You already have a pending request to become a Trainer.");
                }

                var request = new RoleChangeRequest
                {
                    UserID = userId,
                    CurrentRole = UserRoles.Member,
                    RequestedRole = UserRoles.Trainer,
                    Status = RoleChangeStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                };
                await dbContext.RoleChangeRequests.AddAsync(request);
                await dbContext.SaveChangesAsync();

                var adminIds = await dbContext.Users
                    .Where(u => u.UserRoles.Any(ur => ur.Role == UserRoles.Admin && !ur.IsDeleted) && !u.IsDeleted)
                    .Select(u => u.UserID)
                    .ToListAsync();

                foreach (var adminId in adminIds)
                {
                    await dbContext.Notifications.AddAsync(new Notification
                    {
                        UserID = adminId,
                        Title = "New Role Change Request",
                        Content = $"{user.FullName} requested to become a Trainer and is waiting for your approval.",
                        Type = NotificationTypeEnum.RoleChange,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        ActionUrl = ManageUsersActionUrl,
                    });
                }
                await dbContext.SaveChangesAsync();

                return new RoleChangeResultDto
                {
                    IsPendingApproval = true,
                    Message = "Your request to become a Trainer has been sent to the Admin for approval."
                };
            }
            else
            {
                if (!isTrainer)
                {
                    throw new BusinessRuleException("Only Trainer accounts can switch back to Member.");
                }

                var trainerRole = user.UserRoles.First(r => r.Role == UserRoles.Trainer && !r.IsDeleted);
                trainerRole.IsDeleted = true;
                trainerRole.DeletedAt = DateTime.UtcNow;

                if (!isMember)
                {
                    user.UserRoles.Add(new UserRole { Role = UserRoles.Member });
                }

                if (user.MemberProfile == null)
                {
                    user.MemberProfile = new MemberProfile { QRCodeData = Guid.NewGuid().ToString("N") };
                }

                await dbContext.SaveChangesAsync();

                return new RoleChangeResultDto
                {
                    IsPendingApproval = false,
                    Message = "You are now a Member."
                };
            }
        }

        public async Task<List<RoleChangeRequestDto>> GetPendingRoleChangeRequests()
        {
            return await dbContext.RoleChangeRequests
                .Include(r => r.User)
                .Where(r => r.Status == RoleChangeStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .Select(r => new RoleChangeRequestDto
                {
                    RoleChangeRequestID = r.RoleChangeRequestID,
                    UserID = r.UserID,
                    FullName = r.User.FullName,
                    Email = r.User.Email,
                    CurrentRole = r.CurrentRole,
                    RequestedRole = r.RequestedRole,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                })
                .ToListAsync();
        }

        public async Task ApproveRoleChangeRequest(int requestId, int adminUserId, string? note)
        {
            var request = await dbContext.RoleChangeRequests
                .Include(r => r.User).ThenInclude(u => u.UserRoles)
                .Include(r => r.User).ThenInclude(u => u.Trainer)
                .FirstOrDefaultAsync(r => r.RoleChangeRequestID == requestId);

            if (request == null)
            {
                throw new KeyNotFoundException("Role change request not found.");
            }
            if (request.Status != RoleChangeStatus.Pending)
            {
                throw new BusinessRuleException("This request has already been reviewed.");
            }

            var user = request.User;
            bool isAlreadyTrainer = user.UserRoles.Any(r => r.Role == UserRoles.Trainer && !r.IsDeleted);
            if (!isAlreadyTrainer)
            {
                user.UserRoles.Add(new UserRole { Role = UserRoles.Trainer });
            }
            if (user.Trainer == null)
            {
                user.Trainer = new Trainer { Specialization = "N/A", Bio = "N/A", WorkingHours = "N/A" };
            }

            request.Status = RoleChangeStatus.Approved;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedByUserID = adminUserId;
            request.ReviewNote = note;

            await dbContext.Notifications.AddAsync(new Notification
            {
                UserID = user.UserID,
                Title = "Role Change Approved",
                Content = "Your request to become a Trainer has been approved. Welcome to the team!",
                Type = NotificationTypeEnum.RoleChange,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                ActionUrl = "/html/Profile/profile.html",
            });

            await dbContext.SaveChangesAsync();
        }

        public async Task RejectRoleChangeRequest(int requestId, int adminUserId, string? note)
        {
            var request = await dbContext.RoleChangeRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RoleChangeRequestID == requestId);

            if (request == null)
            {
                throw new KeyNotFoundException("Role change request not found.");
            }
            if (request.Status != RoleChangeStatus.Pending)
            {
                throw new BusinessRuleException("This request has already been reviewed.");
            }

            request.Status = RoleChangeStatus.Rejected;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedByUserID = adminUserId;
            request.ReviewNote = note;

            await dbContext.Notifications.AddAsync(new Notification
            {
                UserID = request.UserID,
                Title = "Role Change Rejected",
                Content = string.IsNullOrWhiteSpace(note)
                    ? "Your request to become a Trainer was not approved."
                    : $"Your request to become a Trainer was not approved. Reason: {note}",
                Type = NotificationTypeEnum.RoleChange,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                ActionUrl = "/html/Profile/profile.html",
            });

            await dbContext.SaveChangesAsync();
        }

        public async Task<AuthResponseDto> CreateAdmin(RegisterMemberDto dto, string secretKey)
        {

            const string ExpectedSecretKey = "FitCore_Super_Admin_Secret_2026";

            if (secretKey != ExpectedSecretKey)
            {
                throw new UnauthorizedAccessException("Invalid secret key! You cannot create an admin account.");
            }

            var errors = ValidateBasicInfo(dto.FullName, dto.Email, dto.PhoneNumber, dto.Password);
            if (errors.Any())
            {
                throw new ValidationException(errors);
            }

            if (await dbContext.Users.AnyAsync(u => u.Email == dto.Email && !u.IsDeleted))
            {
                throw new BusinessRuleException("An account with this email already exists.");
            }


            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Status = UserStatus.Active,
                JoinDate = DateTime.UtcNow,
            };


            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);


            user.UserRoles.Add(new UserRole { Role = UserRoles.Admin });

            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            return BuildAuthResponse(user);
        }
    }
}
