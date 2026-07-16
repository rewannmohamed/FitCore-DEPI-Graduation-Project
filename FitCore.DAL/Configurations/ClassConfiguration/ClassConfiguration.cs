using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Configurations.ClassConfiguration
{
    public class ClassConfiguration : IEntityTypeConfiguration<Class>
    {
        public void Configure(EntityTypeBuilder<Class> builder)
        {
            builder.HasKey(c => c.ClassID);
            builder.Property(c => c.ClassName).IsRequired().HasMaxLength(100);

            builder.Property(c => c.Status).HasMaxLength(20);

            builder.HasOne(c => c.Trainer)
                .WithMany(t => t.Classes)
                .HasForeignKey(c => c.TrainerID)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasQueryFilter(b => !b.IsDeleted);
        }
    }
}
