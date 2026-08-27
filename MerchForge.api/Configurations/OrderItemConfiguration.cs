using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", t =>
        {
            t.HasCheckConstraint("CK_order_items_Quantity_Positive", "`Quantity` > 0");
            t.HasCheckConstraint("CK_order_items_UnitPrice_NonNegative", "`UnitPrice` >= 0");
            t.HasCheckConstraint("CK_order_items_LineTotal_NonNegative", "`LineTotal` >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.ProductTitle)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ProductImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.UnitPrice)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.LineTotal)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade: a product that has ever been ordered must not be
        // silently deletable out from under its order history. BusinessDashboardService
        // checks for this up front (HasOrderItemsForProductAsync) and raises a clear
        // ProductHasOrdersException instead of letting this surface as a raw
        // DbUpdateException from a failed DELETE.
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId);

        builder.HasIndex(x => x.ProductId);
    }
}
