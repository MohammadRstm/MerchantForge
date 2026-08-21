using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MerchForge.api.Models;

namespace MerchForge.api.Configurations;

public class ProductConfiguration
    : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", t =>
        {
            // Defense in depth, same reasoning as the json_valid check below: a sale
            // price that isn't actually a discount is a data error, not just a display
            // oddity, so it's worth rejecting at the database rather than trusting
            // every write path to remember the rule.
            t.HasCheckConstraint(
                "CK_products_CompareAtPrice_GreaterThanPrice",
                "`CompareAtPrice` IS NULL OR `CompareAtPrice` > `Price`");

            t.HasCheckConstraint(
                "CK_products_StockQuantity_NonNegative",
                "`StockQuantity` IS NULL OR `StockQuantity` >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Price)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.CompareAtPrice)
            .HasPrecision(10, 2);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Sku)
            .HasMaxLength(100);

        // A real json column, same treatment as Metadata below: stored as a typed
        // List<string> in C#, but json on the wire so it stays queryable/inspectable
        // like every other JSON column here rather than a delimited string. The
        // database-level default matters, not just the C# one: without it, adding
        // this NOT NULL column to a table that already has rows has nothing valid to
        // backfill them with.
        builder.Property(x => x.Tags)
            .HasColumnType("json")
            .HasDefaultValueSql("(JSON_ARRAY())");

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

        // Sku must be unique within a business, not platform-wide — two different
        // merchants numbering their own catalogs "SKU-001" is not a conflict. MySQL/
        // MariaDB unique indexes treat NULL as distinct from every other NULL, so any
        // number of products with no Sku set coexist without tripping this.
        builder.HasIndex(x => new { x.BusinessId, x.Sku }).IsUnique();
    }
}
