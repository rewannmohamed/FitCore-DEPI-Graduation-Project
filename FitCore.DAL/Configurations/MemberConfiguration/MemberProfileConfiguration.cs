using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace FitCore.DAL.Configurations.MemberConfiguration
{
    public class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfile>
    {
        public void Configure(EntityTypeBuilder<MemberProfile> builder)
        {
            builder.HasKey(m => m.MemberProfileId);

            builder.HasOne(m => m.User)
                .WithOne(u => u.MemberProfile)
                .HasForeignKey<MemberProfile>(m => m.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(b => !b.IsDeleted);
        }
    }
}
