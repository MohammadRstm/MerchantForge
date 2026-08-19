using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class BusinessDomainConfiguration : IEntityTypeConfiguration<BusinessDomain>
{
    /// <summary>
    /// Fixed timestamp for seeded rows. HasData must be deterministic — using
    /// DateTime.UtcNow here would make EF detect a model change on every scaffold.
    /// </summary>
    internal static readonly DateTime SeedTimestamp =
        new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal static readonly Guid FashionId =
        Guid.Parse("d1000000-0000-4000-8000-000000000001");

    internal static readonly Guid RestaurantId =
        Guid.Parse("d1000000-0000-4000-8000-000000000002");

    internal static readonly Guid ElectronicsId =
        Guid.Parse("d1000000-0000-4000-8000-000000000003");

    public void Configure(EntityTypeBuilder<BusinessDomain> builder)
    {
        builder.ToTable("business_domains");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Slugs are the public identifier, so they must be unambiguous.
        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.HasData(
            new BusinessDomain
            {
                Id = FashionId,
                Name = "Fashion",
                Slug = "fashion",
                IsActive = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp,
            },
            new BusinessDomain
            {
                Id = RestaurantId,
                Name = "Restaurant",
                Slug = "restaurant",
                IsActive = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp,
            },
            new BusinessDomain
            {
                Id = ElectronicsId,
                Name = "Electronics",
                Slug = "electronics",
                IsActive = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp,
            });
    }
}
