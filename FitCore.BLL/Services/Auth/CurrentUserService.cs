using FitCore.BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace FitCore.BLL.Services.Auth
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        public int? UserId
        {
            get
            {
                var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(claim, out var id) ? id : null;
            }
        }

        public int GetRequiredUserId()
        {
            return UserId ?? throw new UnauthorizedAccessException("User token is invalid or missing.");
        }

        public IReadOnlyList<string> Roles
        {
            get
            {
                if (User == null) return Array.Empty<string>();
                return User.FindAll("role").Select(c => c.Value)
                    .Concat(User.FindAll(ClaimTypes.Role).Select(c => c.Value))
                    .Distinct()
                    .ToList();
            }
        }

        public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
    }
}
