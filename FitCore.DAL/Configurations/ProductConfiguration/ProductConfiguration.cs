using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Configurations.ProductConfiguration
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(x => x.ProductID);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
            
            builder.Property(p => p.CurrentSellPrice)
           .HasColumnType("decimal(18,2)")
           .IsRequired();

            builder.Property(p => p.ImageUrl)
           .HasMaxLength(500);

            builder.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Supplier)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.SupplierID)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasQueryFilter(b => !b.IsDeleted);
        }
    }
}
