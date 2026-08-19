using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MerchForge.api.Models;

namespace MerchForge.api.Configurations;

public class ProductConfiguration
    : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.Price)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(500);

        // A real json column. On MariaDB this resolves to LONGTEXT with an automatic
        // CHECK (json_valid(Metadata)), so invalid JSON is rejected by the database
        // rather than only by application code.
        builder.Property(x => x.Metadata)
            .HasColumnType("json");

        // Deleting a business removes its catalog — that is genuinely owned data.
        builder.HasOne(x => x.Business)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: a category is shared reference data across every business in the
        // domain, so deleting one must never silently delete products.
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BusinessId);

        // Storefront product lists are always business-scoped and usually
        // category-filtered; this composite covers both.
        builder.HasIndex(x => new { x.BusinessId, x.CategoryId });
    }
}
