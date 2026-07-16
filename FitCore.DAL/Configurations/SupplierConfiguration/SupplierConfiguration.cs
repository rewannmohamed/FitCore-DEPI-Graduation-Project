using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Configurations.SupplierConfiguration
{
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.HasKey(c =>c.SupplierID);
            builder.Property(s => s.CompanyName).IsRequired().HasMaxLength(100);
            builder.Property(s => s.SupplierPhone).HasMaxLength(20);
            builder.HasQueryFilter(b => !b.IsDeleted);
        }
    }
}
