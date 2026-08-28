using MerchForge.api.Enums;
using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class SubscriptionPlanConfiguration
    : IEntityTypeConfiguration<SubscriptionPlan>
{
    /// <summary>Fixed so PlanFeatureConfiguration's seed rows can reference it by id.</summary>
    internal static readonly Guid StarterMonthlyId = Guid.Parse("d1000000-0000-4000-8000-000000000001");
    internal static readonly Guid StarterYearlyId = Guid.Parse("d1000000-0000-4000-8000-000000000002");
    internal static readonly Guid GrowthMonthlyId = Guid.Parse("d1000000-0000-4000-8000-000000000003");
    internal static readonly Guid GrowthYearlyId = Guid.Parse("d1000000-0000-4000-8000-000000000004");
    internal static readonly Guid ProMonthlyId = Guid.Parse("d1000000-0000-4000-8000-000000000005");
    internal static readonly Guid ProYearlyId = Guid.Parse("d1000000-0000-4000-8000-000000000006");

    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.BillingInterval)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.IsCustom)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => x.Name);

        builder.HasMany(x => x.PlanFeatures)
            .WithOne(x => x.SubscriptionPlan)
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Subscriptions)
            .WithOne(x => x.SubscriptionPlan)
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Six rows (3 tiers x Monthly/Yearly): BillingInterval is part of a plan's
        // identity here, not a modifier on it, since there's no separate
        // yearly-price field - this fits the model as-is with no schema change.
        // Yearly prices are the monthly rate at the agreed yearly discount, x12.
        builder.HasData(
            new SubscriptionPlan
            {
                Id = StarterMonthlyId,
                Name = "Starter",
                Description = "Unlimited AI product creation, 40 image-edit credits/mo, and basic website branding.",
                Price = 19m,
                Currency = "USD",
                BillingInterval = BillingInterval.Monthly,
                IsActive = true,
                IsCustom = false,
                CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
                UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
            },
            new SubscriptionPlan
            {
                Id = StarterYearlyId,
                Name = "Starter",
                Description = "Unlimited AI product creation, 40 image-edit credits/mo, and basic website branding.",
                Price = 180m,
                Currency = "USD",
                BillingInterval = BillingInterval.Yearly,
                IsActive = true,
                IsCustom = false,
                CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
                UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
            },
            new SubscriptionPlan
            {
                Id = GrowthMonthlyId,
                Name = "Growth",
                Description = "Everything in Starter, plus 150 image-edit credits/mo and advanced website customization.",
                Price = 49m,
                Currency = "USD",
                BillingInterval = BillingInterval.Monthly,
                IsActive = true,
                IsCustom = false,
                CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
                UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
            },
            new SubscriptionPlan
            {
                Id = GrowthYearlyId,
                Name = "Growth",
                Description = "Everything in Starter, plus 150 image-edit credits/mo and advanced website customization.",
                Price = 468m,
                Currency = "USD",
                BillingInterval = BillingInterval.Yearly,
                IsActive = true,
                IsCustom = false,
                CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
                UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
            },
            new SubscriptionPlan
            {
                Id = ProMonthlyId,
                Name = "Pro",
                Description = "Everything in Growth, plus 400 image-edit credits/mo.",
                Price = 99m,
                Currency = "USD",
                BillingInterval = BillingInterval.Monthly,
                IsActive = true,
                IsCustom = false,
                CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
                UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
            },
            new SubscriptionPlan
            {
                Id = ProYearlyId,
                Name = "Pro",
                Description = "Everything in Growth, plus 400 image-edit credits/mo.",
                Price = 948m,
                Currency = "USD",
                BillingInterval = BillingInterval.Yearly,
                IsActive = true,
                IsCustom = false,
                CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
                UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
            });
    }
}