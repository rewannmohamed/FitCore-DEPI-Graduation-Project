using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace FitCore.DAL.Configurations.InvoiceConfiguration
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(i => i.InvoiceID);

            builder.Property(i => i.IssueDate)
                   .HasDefaultValueSql("GETUTCDATE()")
                   .IsRequired();

            builder.Property(i => i.TotalAmount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(i => i.InvoiceStatus)
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(i => i.Description)
                   .HasMaxLength(500);

            builder.HasOne(i => i.User)
                   .WithMany(i => i.Invoices)
                   .HasForeignKey(i => i.UserID)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(b => !b.IsDeleted);

        }
    }
}
