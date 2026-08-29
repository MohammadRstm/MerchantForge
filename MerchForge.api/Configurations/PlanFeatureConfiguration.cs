using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class PlanFeatureConfiguration
    : IEntityTypeConfiguration<PlanFeature>
{
    public void Configure(EntityTypeBuilder<PlanFeature> builder)
    {
        builder.ToTable("plan_features");

        builder.HasKey(x => new
        {
            x.SubscriptionPlanId,
            x.FeatureId
        });

        builder.Property(x => x.Limit)
            .IsRequired(false);

        builder.HasOne(x => x.SubscriptionPlan)
            .WithMany(x => x.PlanFeatures)
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Feature)
            .WithMany(x => x.PlanFeatures)
            .HasForeignKey(x => x.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        // Limit stays null (no cap) for every feature except ai.image_editing,
        // where it's read by the credit-reset job as "credits granted per billing
        // period" - a new meaning layered on this previously-unused column, never
        // consulted by HasPlanFeatureAsync's own binary "is this feature on the
        // plan at all" check.
        builder.HasData(
            // Starter
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.StarterMonthlyId, FeatureId = FeatureConfiguration.AiProductGenerationId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.StarterMonthlyId, FeatureId = FeatureConfiguration.AiImageEditingId, Limit = 40 },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.StarterMonthlyId, FeatureId = FeatureConfiguration.WebsiteCustomizationBasicId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.StarterYearlyId, FeatureId = FeatureConfiguration.AiProductGenerationId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.StarterYearlyId, FeatureId = FeatureConfiguration.AiImageEditingId, Limit = 40 },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.StarterYearlyId, FeatureId = FeatureConfiguration.WebsiteCustomizationBasicId, Limit = null },

            // Growth
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.GrowthMonthlyId, FeatureId = FeatureConfiguration.AiProductGenerationId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.GrowthMonthlyId, FeatureId = FeatureConfiguration.AiImageEditingId, Limit = 150 },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.GrowthMonthlyId, FeatureId = FeatureConfiguration.WebsiteCustomizationBasicId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.GrowthMonthlyId, FeatureId = FeatureConfiguration.WebsiteCustomizationAdvancedId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.GrowthYearlyId, FeatureId = FeatureConfiguration.AiProductGenerationId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.GrowthYearlyId, FeatureId = FeatureConfiguration.AiImageEditingId, Limit = 150 },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.GrowthYearlyId, FeatureId = FeatureConfiguration.WebsiteCustomizationBasicId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.GrowthYearlyId, FeatureId = FeatureConfiguration.WebsiteCustomizationAdvancedId, Limit = null },

            // Pro
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.ProMonthlyId, FeatureId = FeatureConfiguration.AiProductGenerationId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.ProMonthlyId, FeatureId = FeatureConfiguration.AiImageEditingId, Limit = 400 },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.ProMonthlyId, FeatureId = FeatureConfiguration.WebsiteCustomizationBasicId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.ProMonthlyId, FeatureId = FeatureConfiguration.WebsiteCustomizationAdvancedId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.ProYearlyId, FeatureId = FeatureConfiguration.AiProductGenerationId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.ProYearlyId, FeatureId = FeatureConfiguration.AiImageEditingId, Limit = 400 },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.ProYearlyId, FeatureId = FeatureConfiguration.WebsiteCustomizationBasicId, Limit = null },
            new PlanFeature { SubscriptionPlanId = SubscriptionPlanConfiguration.ProYearlyId, FeatureId = FeatureConfiguration.WebsiteCustomizationAdvancedId, Limit = null });
    }
}