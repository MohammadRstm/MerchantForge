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
    }
}