using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitCore.DAL.Configurations.PrivateSessionConfiguration
{
    public class PrivateSessionConfiguration : IEntityTypeConfiguration<PrivateSession>
    {
        public void Configure(EntityTypeBuilder<PrivateSession> builder)
        {
            builder.HasKey(p => p.PrivateSessionID);

            builder.Property(p => p.SessionDate).IsRequired();
            builder.Property(p => p.StartTime).IsRequired();
            builder.Property(p => p.EndTime).IsRequired();
            builder.Property(p => p.Notes).HasMaxLength(500);
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()").IsRequired();

            builder.HasOne(p => p.Trainer)
                .WithMany(t => t.PrivateSessions)
                .HasForeignKey(p => p.TrainerID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.MemberProfile)
                .WithMany(mp => mp.PrivateSessions)
                .HasForeignKey(p => p.MemberUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasQueryFilter(b => !b.IsDeleted);
        }
    }
}
