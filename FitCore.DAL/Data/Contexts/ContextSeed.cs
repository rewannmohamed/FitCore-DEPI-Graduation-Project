using FitCore.DAL.Data.Contexts;
using FitCore.DAL.Data.Models;
using FitCore.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FitCore.DAL.Data
{
    public static class ContextSeed
    {
        public static async Task SeedAllAsync(FitCoreDbContext context)
        {
            await context.Database.EnsureCreatedAsync();

            if (!await context.Users.AnyAsync())
            {
                var adminUser = new User
                {
                    FullName = "Youssef Hamdy",
                    Email = "admin@fitcore.com",
                    PasswordHash = "123",
                    PhoneNumber = "01000000000",
                    Status = UserStatus.Active,
                    JoinDate = DateTime.UtcNow
                };

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();
            }

            if (!await context.GymServices.AnyAsync())
            {
                var defaultServices = new List<GymService>
                {
                    new GymService
                    {
                        Name = "Premium Monthly Membership",
                        Price = 600,
                        DurationInDays = 30,
                        Category = ServiceCategory.Membership 
                    },
                    new GymService
                    {
                        Name = "VIP Yearly Package",
                        Price = 5000,
                        DurationInDays = 365,
                        Category = ServiceCategory.Package 
                    }
                };
                await context.GymServices.AddRangeAsync(defaultServices);
                await context.SaveChangesAsync();
            }
        }
    }
}