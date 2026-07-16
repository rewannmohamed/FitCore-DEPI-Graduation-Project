using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitCore.DAL.Configurations.TrainerConfiguration
{
    public class TrainerWorkingHourConfiguration : IEntityTypeConfiguration<TrainerWorkingHour>
    {
        public void Configure(EntityTypeBuilder<TrainerWorkingHour> builder)
        {
            builder.HasKey(w => w.Id);

            builder.Property(w => w.Day).IsRequired();
            builder.Property(w => w.StartTime).IsRequired();
            builder.Property(w => w.EndTime).IsRequired();

            builder.HasOne(w => w.Trainer)
                .WithMany(t => t.WorkingHoursSchedule)
                .HasForeignKey(w => w.TrainerID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(b => !b.IsDeleted);
        }
    }
}
