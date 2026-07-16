using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace FitCore.DAL.Configurations.TransactionsConfiguration
{
    public class InventoryTransactionItemConfiguration : IEntityTypeConfiguration<InventoryTransactionItem>
    {
        public void Configure(EntityTypeBuilder<InventoryTransactionItem> builder)
        {
            builder.HasKey(ti => ti.TransactionItemID);

            builder.Property(ti => ti.Quantity)
                   .IsRequired();

            builder.Property(ti => ti.BatchNumber)
                   .HasMaxLength(50);

            builder.Property(p => p.UnitCost)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

            builder.HasOne(ti => ti.Transaction)
                   .WithMany(t => t.TransactionItems)
                   .HasForeignKey(ti => ti.TransactionID)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ti => ti.Product)
                   .WithMany(ti => ti.InventoryTransactionsItems)
                   .HasForeignKey(ti => ti.ProductID)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);
        }
    }
}
