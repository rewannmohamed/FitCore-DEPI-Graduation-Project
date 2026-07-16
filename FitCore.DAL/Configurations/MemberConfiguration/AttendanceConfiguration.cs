using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Configurations.MemberConfiguration
{
    public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
    {
        public void Configure(EntityTypeBuilder<Attendance> builder)
        {
            builder.HasKey(a => a.AttendanceID);
            
            builder.Property(a => a.CheckInTime)
               .HasDefaultValueSql("GETUTCDATE()")
               .IsRequired();
            
            builder.HasOne(a => a.MemberProfile)
                .WithMany(mp => mp.Attendances)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(a => a.Membership)
                   .WithMany(m => m.Attendances)
                   .HasForeignKey(a => a.MembershipID)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasQueryFilter(b => !b.IsDeleted);
        }
    }
}
