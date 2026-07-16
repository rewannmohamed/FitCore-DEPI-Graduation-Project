using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitCore.DAL.Configurations.ClassConfiguration
{
    public class ClassScheduleConfiguration : IEntityTypeConfiguration<ClassSchedule>
    {
        public void Configure(EntityTypeBuilder<ClassSchedule> builder)
        {
            builder.HasKey(cs => cs.Id);

            builder.Property(cs => cs.StartTime).IsRequired();
            builder.Property(cs => cs.EndTime).IsRequired();
            builder.Property(cs => cs.Day).IsRequired();

            builder.HasOne(cs => cs.Class)
                   .WithMany(c => c.Schedules)
                   .HasForeignKey(cs => cs.ClassID)
                   .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasQueryFilter(b => !b.IsDeleted);
        }
    }
}
