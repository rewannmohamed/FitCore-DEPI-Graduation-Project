using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Configurations.TransactionsConfiguration
{
    public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.HasKey(t => t.TransactionID);

            builder.Property(t => t.Type)
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(t => t.TransactionDate)
                   .HasDefaultValueSql("GETUTCDATE()")
                   .IsRequired();

            builder.Property(t => t.ReferenceNumber)
                   .HasMaxLength(50);

            builder.Property(t => t.Notes)
                   .HasMaxLength(500);

            builder.HasOne(t => t.User)
                   .WithMany() 
                   .HasForeignKey(t => t.UserID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
