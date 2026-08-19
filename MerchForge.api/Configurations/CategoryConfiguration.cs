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

        // Slug is unique per domain, not globally — "accessories" legitimately exists
        // under both Fashion and Electronics.
        builder.HasIndex(x => new { x.BusinessDomainId, x.Slug })
            .IsUnique();

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
