using MerchForge.api.Enums;
using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class FeatureConfiguration
    : IEntityTypeConfiguration<Feature>
{
    /// <summary>Fixed so FeatureCreditPackageConfiguration's seed rows can reference it by id.</summary>
    internal static readonly Guid AiProductGenerationId =
        Guid.Parse("f0000000-0000-4000-8000-000000000001");

    /// <summary>Fixed so FeatureCreditPackageConfiguration's seed rows can reference it by id.</summary>
    internal static readonly Guid AiImageEditingId =
        Guid.Parse("f0000000-0000-4000-8000-000000000002");

    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.ToTable("features");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.Key)
            .IsUnique();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.SupportsCreditPurchase)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasData(
            new Feature
            {
                Id = AiProductGenerationId,
                Key = FeatureKeys.AiProductGeneration,
                Name = "AI Product Creation",
                Description = "Draft a product through conversation instead of filling in the form by hand.",
                IsActive = true,
                SupportsCreditPurchase = true,
                CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
                UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
            },
            new Feature
            {
                Id = AiImageEditingId,
                Key = FeatureKeys.AiImageEditing,
                Name = "AI Image Editing",
                Description = "Edit a product photo by describing the change you want instead of using an image editor.",
                IsActive = true,
                SupportsCreditPurchase = true,
                CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
                UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
            });
    }
}