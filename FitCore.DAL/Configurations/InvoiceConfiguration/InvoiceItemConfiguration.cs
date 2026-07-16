using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Configurations.InvoiceConfiguration
{
    public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
    {
        public void Configure(EntityTypeBuilder<InvoiceItem> builder)
        {
            builder.HasKey(i => i.InvoiceItemID);

            builder.Property(i => i.ItemType)
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(i => i.ItemName)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(i => i.Quantity)
                   .IsRequired()
                   .HasDefaultValue(1);

            builder.Property(i => i.SellPrice)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(i => i.LineTotal)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.HasOne(i => i.Invoice)
                   .WithMany(inv => inv.InvoiceItems)
                   .HasForeignKey(i => i.InvoiceID)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.Product)
                   .WithMany( i=> i.InvoiceItems)
                   .HasForeignKey(i => i.ProductID)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasOne(i => i.GymService)
                   .WithMany(i => i.InvoicesItems)
                   .HasForeignKey(i => i.ServiceID)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasOne(i => i.Class)
           .WithMany(i => i.InvoicesItems)
           .HasForeignKey(i => i.ClassID)
           .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(t => t.HasCheckConstraint("CK_InvoiceItem_TypeAllowed",
                "(CASE WHEN ProductID IS NOT NULL THEN 1 ELSE 0 END + " +
                " CASE WHEN ServiceID IS NOT NULL THEN 1 ELSE 0 END + " +
                " CASE WHEN ClassID IS NOT NULL THEN 1 ELSE 0 END) = 1"));

            builder.HasQueryFilter(b => !b.IsDeleted);
        }
    }
}
