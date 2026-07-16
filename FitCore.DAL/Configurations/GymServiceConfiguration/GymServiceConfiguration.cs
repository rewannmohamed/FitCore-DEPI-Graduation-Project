using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class GymServiceConfiguration : IEntityTypeConfiguration<GymService>
{
    public void Configure(EntityTypeBuilder<GymService> builder)
    {
        builder.HasKey(g => g.ServiceID);

        builder.Property(g => g.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(g => g.Price)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        builder.Property(g => g.Category)
               .HasConversion<string>()
               .IsRequired();

        builder.Property(g => g.DurationInDays)
               .IsRequired()
               .HasDefaultValue(0);

        builder.HasQueryFilter(b => !b.IsDeleted);


    }
}