using FitCore.BLL.Interfaces.Auth;
using FitCore.DAL.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FitCore.BLL.Services.Auth
{
    public class JwtTokenGenerator(IConfiguration _configuration) : IJwtTokenGenerator
    {
        public (string Token, DateTime ExpiresAt) GenerateToken(User user, IEnumerable<string> roles)
        {
            var jwtSection = _configuration.GetSection("JWT");
            var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.");
            var issuer = jwtSection["ValidIssuer"];
            var audience = jwtSection["ValidAudience"];

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
            };


            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddHours(8);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return (tokenString, expiresAt);
        }
    }
}
