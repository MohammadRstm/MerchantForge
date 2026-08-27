using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements", t =>
        {
            t.HasCheckConstraint(
                "CK_stock_movements_Amount_NotZero",
                "`Amount` <> 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.BusinessId)
            .IsRequired();

        builder.Property(x => x.Amount)
            .IsRequired();

        builder.Property(x => x.BalanceAfter)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(255);

        builder.Property(x => x.CreatedByUserId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.ProductId);

        // Covers the recent-activity list: business-scoped, newest first.
        builder.HasIndex(x => new { x.BusinessId, x.CreatedAt });

        // One-directional — no Product.StockMovements/Business.StockMovements
        // navigation is added, same as FeatureCreditTransactionConfiguration's
        // HasOne<FeatureCreditPackage>().WithMany() for its package FK.
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Business>()
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
