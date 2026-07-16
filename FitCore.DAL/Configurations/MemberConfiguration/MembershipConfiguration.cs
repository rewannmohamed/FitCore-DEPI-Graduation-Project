using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FitCore.DAL.Configurations
{
    public class MembershipConfiguration : IEntityTypeConfiguration<Membership>
    {
        public void Configure(EntityTypeBuilder<Membership> builder)
        {
            builder.HasKey(x => x.MembershipID);

            builder.HasOne(m => m.MemberProfile)
                   .WithMany(mp => mp.Memberships)
                   .HasForeignKey(m => m.MemberProfileId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.GymService)
                   .WithMany(g => g.Memberships)
                   .HasForeignKey(m => m.GymServiceId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Class)
                   .WithMany(c => c.Memberships)
                   .HasForeignKey(m => m.ClassID)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Invoice).WithMany(x => x.Memberships).HasForeignKey(x => x.InvoiceID).OnDelete(DeleteBehavior.Restrict);

            builder.Property(m => m.RemainingSessions)
                   .IsRequired(false);

            builder.HasQueryFilter(b => !b.IsDeleted);
        }
    }
}
