using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitCore.DAL.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("Bookings", t =>
            {
                t.HasCheckConstraint("CK_Booking_ClassOrService_Exclusive",
                    "(ClassID IS NOT NULL AND GymServiceId IS NULL) OR (ClassID IS NULL AND GymServiceId IS NOT NULL)");
            });

            builder.HasKey(b => b.BookingID);

            builder.Property(b => b.Status)
                   .IsRequired();

            builder.Property(b => b.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(b => b.IsDeleted)
                   .HasDefaultValue(false);

            builder.HasQueryFilter(b => !b.IsDeleted);

            builder.HasOne(b => b.Class)
                   .WithMany(b => b.Bookings)
                   .HasForeignKey(b => b.ClassID)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.GymService)
                   .WithMany(b  => b.Bookings)
                   .HasForeignKey(b => b.GymServiceId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.MemberProfile)
                   .WithMany(b => b.Bookings)
                   .HasForeignKey(b => b.MemberUserId)
                   .OnDelete(DeleteBehavior.Restrict);
            
        }
    }
}