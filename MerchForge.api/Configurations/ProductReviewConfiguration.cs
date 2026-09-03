using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("product_reviews", t =>
        {
            // Enforced in the database as well as in CreateProductReviewRequestValidator:
            // a rating outside 1-5 would silently skew every average computed from this
            // table, and averages are read far more often than reviews are written.
            t.HasCheckConstraint(
                "CK_product_reviews_Rating_Range",
                "`Rating` BETWEEN 1 AND 5");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.BusinessId)
            .IsRequired();

        builder.Property(x => x.CustomerId)
            .IsRequired();

        builder.Property(x => x.Rating)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(2000);

        builder.Property(x => x.IsHidden)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // One review per customer per product, enforced by the database rather than by
        // a check-then-insert in the service, which would race. The write path upserts
        // against this pair instead of inserting blindly.
        builder.HasIndex(x => new { x.ProductId, x.CustomerId })
            .IsUnique()
            .HasDatabaseName("UX_product_reviews_OnePerCustomerPerProduct");

        // The storefront list: one product's visible reviews, newest first.
        builder.HasIndex(x => new { x.ProductId, x.IsHidden, x.CreatedAt });

        // The owner's moderation views are business-scoped, newest first.
        builder.HasIndex(x => new { x.BusinessId, x.CreatedAt });

        // One-directional for Product and Business — no Product.Reviews collection, so
        // that loading a product never risks dragging its whole review history with it.
        // Same shape as StockMovementConfiguration's two FKs.
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Business>()
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cascade, unlike Order's SetNull for the same FK: an order must outlive the
        // customer who placed it because it's a business's financial record, but a
        // review is that customer's own words about a product. Deleting the account
        // takes the reviews with it, and the nav property is kept because every read
        // projects the author's display name from it.
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
