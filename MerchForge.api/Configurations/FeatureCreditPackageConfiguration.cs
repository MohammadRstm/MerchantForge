using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class FeatureCreditPackageConfiguration
    : IEntityTypeConfiguration<FeatureCreditPackage>
{
    public void Configure(EntityTypeBuilder<FeatureCreditPackage> builder)
    {
        builder.ToTable("feature_credit_packages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FeatureId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Credits)
            .IsRequired();

        builder.Property(x => x.Price)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => x.FeatureId);

        builder.HasOne(x => x.Feature)
            .WithMany(x => x.CreditPackages)
            .HasForeignKey(x => x.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        // Placeholder pricing - easy to change later since nothing else is keyed to
        // these exact numbers, only to the package id a purchase references. Image
        // editing is priced at fewer credits per dollar than product generation: a
        // single edit call costs meaningfully more than a text turn.
        builder.HasData(
            new FeatureCreditPackage
            {
                Id = Guid.Parse("f0000000-0000-4000-8000-000000000101"),
                FeatureId = FeatureConfiguration.AiProductGenerationId,
                Name = "Starter",
                Credits = 50,
                Price = 5m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
                UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
            },
            new FeatureCreditPackage
            {
                Id = Guid.Parse("f0000000-0000-4000-8000-000000000102"),
                FeatureId = FeatureConfiguration.AiProductGenerationId,
                Name = "Pro",
                Credits = 200,
                Price = 15m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
                UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
            },
            new FeatureCreditPackage
            {
                Id = Guid.Parse("f0000000-0000-4000-8000-000000000201"),
                FeatureId = FeatureConfiguration.AiImageEditingId,
                Name = "Starter",
                Credits = 20,
                Price = 5m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
                UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
            },
            new FeatureCreditPackage
            {
                Id = Guid.Parse("f0000000-0000-4000-8000-000000000202"),
                FeatureId = FeatureConfiguration.AiImageEditingId,
                Name = "Pro",
                Credits = 100,
                Price = 20m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
                UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
            });
    }
}
