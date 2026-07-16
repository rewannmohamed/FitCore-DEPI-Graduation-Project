using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitCore.DAL.Configurations.UserConfiguration
{
    public class RoleChangeRequestConfiguration : IEntityTypeConfiguration<RoleChangeRequest>
    {
        public void Configure(EntityTypeBuilder<RoleChangeRequest> builder)
        {
            builder.HasKey(r => r.RoleChangeRequestID);

            builder.Property(r => r.ReviewNote).HasMaxLength(300);

            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.ReviewedByUser)
                .WithMany()
                .HasForeignKey(r => r.ReviewedByUserID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
