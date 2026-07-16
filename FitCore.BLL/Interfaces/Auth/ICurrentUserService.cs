using System;
using System.Collections.Generic;

namespace FitCore.BLL.Interfaces.Auth
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        int GetRequiredUserId();
        IReadOnlyList<string> Roles { get; }
        bool IsInRole(string role);
        bool IsAuthenticated { get; }
    }
}
