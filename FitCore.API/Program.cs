using FitCore.API.Middlewares;
using FitCore.BLL.Interfaces.AdminDashboard;
using FitCore.BLL.Interfaces.Attendance;
using FitCore.BLL.Interfaces.AuditLogs;
using FitCore.BLL.Interfaces.Classes;
using FitCore.BLL.Interfaces.IShopService;
using FitCore.BLL.Interfaces.MemberDashboard;
using FitCore.DAL.Data.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using FitCore.BLL.Interfaces.Auth;
using FitCore.BLL.Services.Auth;
using System.Security.Claims;
using System.Text;
using FitCore.BLL.Interfaces.Membership;
using FitCore.BLL.Interfaces.Notifications;
using FitCore.BLL.Interfaces.PrivateSessions;
using FitCore.BLL.Interfaces.Profile;
using FitCore.BLL.Services;
using FitCore.BLL.Interfaces.Trainers;
using FitCore.BLL.Services.Attendance;
using FitCore.BLL.Services.AuditLogs;
using FitCore.BLL.Services.Classes;
using FitCore.BLL.Services.Notifications;
using FitCore.BLL.Services.PrivateSessions;
using FitCore.BLL.Services.Profile;
using FitCore.BLL.Services.Trainers;
using FitCore.DAL.Data;
using FitCore.DAL.Data.Contexts;
using FitCore.DAL.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using FitCore.BLL.Interfaces.GymService;
using FitCore.BLL.Services.GymServices;
using FitCore.BLL.Interfaces.Payment;
using FitCore.BLL.Services.Invoices;

namespace FitCore.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {

            System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            var builder = WebApplication.CreateBuilder(args);

            // 1. Connection String & DbContext
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            Console.WriteLine("=================================");
            Console.WriteLine(connectionString ?? "NULL");
            Console.WriteLine("=================================");

            builder.Services.AddDbContext<FitCoreDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IAuditLogsService, AuditLogService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IClassService, ClassService>();
            builder.Services.AddScoped<ITrainerService, TrainerService>();
            builder.Services.AddScoped<IPrivateSessionService, PrivateSessionService>();
            builder.Services.AddHttpContextAccessor(); 
            builder.Services.AddScoped<IMembershipService, MembershipService>();
            builder.Services.AddScoped<IGymServiceService, GymServiceService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IAttendanceService, AttendanceService>();
            builder.Services.AddScoped<IMemberDashboardService, MemberDashboardService>();
            builder.Services.AddScoped<IShopService, ShopService>();
            builder.Services.AddScoped<FitCore.BLL.Interfaces.Category.ICategoryService, FitCore.BLL.Services.Category.CategoryService>();
            
            // Add Checkout and Subscription services
            builder.Services.AddScoped<FitCore.BLL.Services.CheckoutService>();
            builder.Services.AddHttpContextAccessor();
            // Add Checkout and Subscription services
            builder.Services.AddScoped<ICheckoutService, CheckoutService>();


            Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
            builder.Services.AddScoped<IPaymentService,PaymentService>();
            builder.Services.AddScoped<IInvoiceService,InvoiceService>();
            #region Added Hangfire

            builder.Services.AddHangfire(config => config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer() //when create job use simple service name not full name with version and Public Key Token
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHangfireServer();
            #endregion

            // Auth
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            var jwtSection = builder.Configuration.GetSection("JWT");
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["ValidIssuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["ValidAudience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Secret"]!)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });
            builder.Services.AddAuthorization();

            builder.Services.AddControllers();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });


            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.CustomSchemaIds(type => type.FullName);
            });

            var app = builder.Build();

            app.UseMiddleware<GlobalExceptionMiddleware>();

            using (var scope = app.Services.CreateScope())
            {
                var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var membershipService = scope.ServiceProvider.GetRequiredService<IMembershipService>();

                recurringJobManager.AddOrUpdate(
                    "check-MemberShip-expiration",
                    () => notificationService.MemberExpiryNotification(),
                    Cron.Daily(1)
                );
                recurringJobManager.AddOrUpdate(
                    "check-Low-Stock",
                    () => notificationService.LowStockNotification(),
                    Cron.Daily(3)
                );
                recurringJobManager.AddOrUpdate(
                    "check-Near-Expiry-Products",
                    () => notificationService.ExpiryProductsNotification(),
                    Cron.Daily(5)
                );
                recurringJobManager.AddOrUpdate(
                    "check-Froze-MemberShips",
                    () => membershipService.AutoUnfreezeExpiredFreezesAsync(),
                    Cron.Daily(6)
                );
            }
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseHangfireDashboard("/hangfire");
            }          
            app.UseHttpsRedirection();

            app.UseCors("AllowFrontend");

            app.UseAuthentication(); 
            app.UseAuthorization();            
            
            app.MapControllers();

            var shouldSeed = builder.Configuration.GetValue<bool>("SeedData");

            if (shouldSeed)
            {
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        var context = services.GetRequiredService<FitCoreDbContext>();

                        await ContextSeed.SeedAllAsync(context);
                    }
                    catch (Exception ex)
                    {
                        var logger = services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "حصلت مشكلة أثناء تنزيل الداتا الافتراضية (Seeding).");
                    }
                }
            }

            app.Run();
        }
    }
}