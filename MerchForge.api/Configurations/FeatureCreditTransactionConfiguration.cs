using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class FeatureCreditTransactionConfiguration
    : IEntityTypeConfiguration<FeatureCreditTransaction>
{
    public void Configure(EntityTypeBuilder<FeatureCreditTransaction> builder)
    {
        builder.ToTable("feature_credit_transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessFeatureCreditId)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Amount)
            .IsRequired();

        builder.Property(x => x.BalanceAfter)
            .IsRequired();

        builder.Property(x => x.Reference)
            .HasMaxLength(255);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.BusinessFeatureCreditId);

        builder.HasOne(x => x.BusinessFeatureCredit)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.BusinessFeatureCreditId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<FeatureCreditPackage>()
            .WithMany()
            .HasForeignKey(x => x.FeatureCreditPackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
