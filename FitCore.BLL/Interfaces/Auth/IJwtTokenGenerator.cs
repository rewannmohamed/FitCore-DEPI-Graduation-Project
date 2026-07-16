using FitCore.DAL.Data.Models;
using System;
using System.Collections.Generic;

namespace FitCore.BLL.Interfaces.Auth
{
    public interface IJwtTokenGenerator
    {
        /// <summary>بيبني JWT فيه Claim للـ Role لكل Role المستخدم عنده (ممكن يكون أكتر من واحد).</summary>
        (string Token, DateTime ExpiresAt) GenerateToken(User user, IEnumerable<string> roles);
    }
}
