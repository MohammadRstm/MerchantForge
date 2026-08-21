using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class BusinessFeatureCreditConfiguration
    : IEntityTypeConfiguration<BusinessFeatureCredit>
{
    public void Configure(EntityTypeBuilder<BusinessFeatureCredit> builder)
    {
        builder.ToTable("business_feature_credits");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessId)
            .IsRequired();

        builder.Property(x => x.FeatureId)
            .IsRequired();

        builder.Property(x => x.CreditsRemaining)
            .IsRequired();

        builder.Property(x => x.CreditsGrantedTotal)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // One balance per (business, feature) - a purchase tops this up rather than
        // creating a second row, so this must be enforced at the database.
        builder.HasIndex(x => new { x.BusinessId, x.FeatureId })
            .IsUnique();

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Feature)
            .WithMany()
            .HasForeignKey(x => x.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
