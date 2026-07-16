using FitCore.DAL.Data.Models;
using FitCore.DAL.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Contexts
{
    public class FitCoreDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly HashSet<string> SensitiveProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash",
            "Password",
            "SecurityStamp",
            "ConcurrencyStamp",
            "TwoFactorSecret",
            "RefreshToken",
            "AccessToken",
            "NormalizedEmail",
            "NormalizedUserName"
        };

        public FitCoreDbContext(DbContextOptions<FitCoreDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor; 
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<MemberProfile> MemberProfiles { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<ClassSchedule> ClassSchedule { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<InventoryTransactionItem> InventoryTransactionsItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<GymService> GymServices { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<PrivateSession> PrivateSessions { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<TrainerWorkingHour> TrainerWorkingHours { get; set; }
        public DbSet<RoleChangeRequest> RoleChangeRequests { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var currentTime = DateTime.UtcNow;
            var userIdClaim = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = int.TryParse(userIdClaim, out var id) ? id : null;
            
            //var userId = 1;


            var auditEntries = new List<(AuditLog audit, EntityEntry entry)>();

            var entries = ChangeTracker.Entries().Where(e => e is
            {
                Entity: IAuditable,
                State: EntityState.Added or EntityState.Modified or EntityState.Deleted,
            }).ToList();


            foreach (var entry in entries)
            {
                var audit = CreateAuditTrialFromEntry(entry, currentTime, userId);
                auditEntries.Add((audit, entry));
            }

            if (!auditEntries.Any())
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            await AuditLogs.AddRangeAsync(auditEntries.Select(x => x.audit), cancellationToken);

            var result = await base.SaveChangesAsync(cancellationToken);

            foreach (var (audit, entry) in auditEntries.Where(x => x.audit.Action == "Insert"))
            {
                var pkProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                if (pkProperty?.CurrentValue != null)
                {
                    audit.EntityPrimaryKey = int.TryParse(pkProperty.CurrentValue.ToString(), out var pk)
                        ? pk : throw new ArgumentException("primary key needs to be present");
                }
            }
            if (auditEntries.Any(x => x.audit.Action == "Insert"))
            {
                await base.SaveChangesAsync(cancellationToken);
            }
            return result;
        }

        private AuditLog CreateAuditTrialFromEntry(EntityEntry entry, DateTime currentTime, int? userId)
        {
            var auditTrial = new AuditLog()
            {
                EntityName = entry.Entity.GetType().Name,
                Action = GetAction(entry.State),
                CreatedAt = currentTime,
                UserId = userId,
            };
            var OldValues = new Dictionary<string, object>();
            var NewValues = new Dictionary<string, object>();

            foreach (var property in entry.Properties.Where(x => !x.IsTemporary))
            {
                if (property.Metadata.IsPrimaryKey())
                {
                    auditTrial.EntityPrimaryKey = int.TryParse(property.CurrentValue?.ToString(), out var pk)
                        ? pk : 0;
                    continue;
                }
                if (!ShouldAuditProperty(property))
                {
                    continue;
                }
                AddPropertyValuesBasedOnState(entry.State, property, OldValues, NewValues);
            }

            auditTrial.OldValue = OldValues.Any() ? JsonSerializer.Serialize(OldValues) : null;
            auditTrial.NewValue = NewValues.Any() ? JsonSerializer.Serialize(NewValues) : null;

            return auditTrial;
        }

        private static void AddPropertyValuesBasedOnState(EntityState state, PropertyEntry property, Dictionary<string, object> oldValues, Dictionary<string, object> newValues)
        {
            var propertyName = property.Metadata.Name;

            switch (state)
            {
                case EntityState.Added:
                    newValues[propertyName] = property.CurrentValue!;
                    break;

                case EntityState.Modified when property.IsModified:
                    oldValues[propertyName] = property.OriginalValue!;
                    newValues[propertyName] = property.CurrentValue!;
                    break;

                case EntityState.Deleted:
                    oldValues[propertyName] = property.OriginalValue!;
                    break;

            }
        }

        private static bool ShouldAuditProperty(PropertyEntry entry)
        {
            return !SensitiveProperties.Contains(entry.Metadata.Name);
        }

        private string GetAction(EntityState state)
            => state switch
            {
                EntityState.Added => "Insert",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => "UnKnown"
            };
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FitCoreDbContext).Assembly);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(nameof(ISoftDelete.IsDeleted))
                        .HasDefaultValue(false);

                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var filterBody = Expression.Equal(
                        Expression.Property(parameter, nameof(ISoftDelete.IsDeleted)),
                        Expression.Constant(false)
                    );
                    var lambda = Expression.Lambda(filterBody, parameter);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }

        }
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        }
    }
}
