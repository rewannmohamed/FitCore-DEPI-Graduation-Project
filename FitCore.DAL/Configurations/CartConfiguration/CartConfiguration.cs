using FitCore.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.HasKey(c => c.CartID);

        builder.HasOne(c => c.User)
               .WithOne(u => u.Cart) 
               .HasForeignKey<Cart>(c => c.UserID)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(ci => ci.CartItemID);

        builder.Property(ci => ci.Quantity)
               .IsRequired()
               .HasDefaultValue(1);

        builder.HasOne(ci => ci.Cart)
               .WithMany(c => c.CartItems)
               .HasForeignKey(ci => ci.CartID)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ci => ci.Product)
               .WithMany()
               .HasForeignKey(ci => ci.ProductID)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}