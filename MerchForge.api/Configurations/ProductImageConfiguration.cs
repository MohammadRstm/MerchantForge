using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MerchForge.api.Models;

namespace MerchForge.api.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.AltText)
            .HasMaxLength(255);

        // A product's images are genuinely owned by it — deleting the product should
        // delete its gallery, same reasoning as Product's own cascade from Business.
        builder.HasOne(x => x.Product)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Galleries are always read as "all images for this product, in order."
        builder.HasIndex(x => new { x.ProductId, x.DisplayOrder });

        // "At most one main image per product" enforced by the database, not just
        // application code: a generated column that resolves to ProductId only on the
        // row where IsMain is true, and is NULL everywhere else. MySQL/MariaDB unique
        // indexes treat every NULL as distinct, so the index only actually constrains
        // the IsMain=true rows — a second one for the same product collides on this
        // column and is rejected, while any number of non-main images coexist freely.
        builder.Property<Guid?>("MainImageProductId")
            .HasComputedColumnSql("(CASE WHEN `IsMain` = 1 THEN `ProductId` ELSE NULL END)", stored: true);

        builder.HasIndex("MainImageProductId")
            .IsUnique()
            .HasDatabaseName("UX_product_images_OneMainPerProduct");
    }
}
