using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    private static Category Seed(
        string id,
        Guid domainId,
        string name,
        string slug,
        int displayOrder) => new()
        {
            Id = Guid.Parse(id),
            BusinessDomainId = domainId,
            Name = name,
            Slug = slug,
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
            UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
        };

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DisplayOrder)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Restrict, not Cascade: retiring a domain must not silently delete the
        // categories products still point at.
        builder.HasOne(x => x.BusinessDomain)
            .WithMany(x => x.Categories)
            .HasForeignKey(x => x.BusinessDomainId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict, not Cascade: there is no business-deletion feature yet, and
        // MariaDB's ordering across two independent CASCADE paths from the same
        // parent (Business -> Product and Business -> Category, with Product ->
        // Category itself RESTRICTed) is not something to depend on unverified. If
        // business deletion is ever built, its own custom categories need to be
        // reassigned or deleted explicitly first — same as it already must handle
        // Product -> Category RESTRICT.
        builder.HasOne(x => x.Business)
            .WithMany(x => x.CustomCategories)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        // Slug is unique per (domain, owning business) — NOT per domain alone.
        // BusinessId is null for platform categories, so two different businesses can
        // each have their own private "vintage" category without colliding: their
        // rows differ on BusinessId, which is the whole point.
        //
        // Caveat: MariaDB (standard SQL) treats every NULL as distinct in a unique
        // index, so this does NOT stop two platform (BusinessId IS NULL) categories
        // from sharing a slug within a domain. That's accepted here rather than
        // worked around (e.g. a generated column substituting a sentinel for NULL):
        // platform categories are only ever created by seeding, which is under this
        // codebase's own control, not by any runtime request path.
        builder.HasIndex(x => new { x.BusinessDomainId, x.BusinessId, x.Slug })
            .IsUnique();

        builder.HasIndex(x => x.BusinessId);

        builder.HasData(
            // Fashion
            Seed("c1000000-0000-4000-8000-000000000001", BusinessDomainConfiguration.FashionId, "Shoes", "shoes", 1),
            Seed("c1000000-0000-4000-8000-000000000002", BusinessDomainConfiguration.FashionId, "Shirts", "shirts", 2),
            Seed("c1000000-0000-4000-8000-000000000003", BusinessDomainConfiguration.FashionId, "Accessories", "accessories", 3),

            // Restaurant
            Seed("c2000000-0000-4000-8000-000000000001", BusinessDomainConfiguration.RestaurantId, "Pizza", "pizza", 1),
            Seed("c2000000-0000-4000-8000-000000000002", BusinessDomainConfiguration.RestaurantId, "Burgers", "burgers", 2),
            Seed("c2000000-0000-4000-8000-000000000003", BusinessDomainConfiguration.RestaurantId, "Drinks", "drinks", 3),

            // Electronics
            Seed("c3000000-0000-4000-8000-000000000001", BusinessDomainConfiguration.ElectronicsId, "Phones", "phones", 1),
            Seed("c3000000-0000-4000-8000-000000000002", BusinessDomainConfiguration.ElectronicsId, "Laptops", "laptops", 2),
            Seed("c3000000-0000-4000-8000-000000000003", BusinessDomainConfiguration.ElectronicsId, "Accessories", "accessories", 3));
    }
}
