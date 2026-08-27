using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MerchForge.api.Models;

namespace MerchForge.api.Configurations;

public class BusinessConfigurations : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("businesses", t =>
        {
            t.HasCheckConstraint(
                "CK_businesses_LowStockThreshold_Positive",
                "`LowStockThreshold` >= 1");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict: retiring a domain must not cascade into deleting businesses.
        builder.HasOne(x => x.BusinessDomain)
            .WithMany(x => x.Businesses)
            .HasForeignKey(x => x.BusinessDomainId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict: a SuperAdmin retiring a template must not cascade into wiping
        // out which template the businesses already using it are on.
        builder.HasOne(x => x.WebsiteTemplate)
            .WithMany(x => x.Businesses)
            .HasForeignKey(x => x.WebsiteTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        // Storefront configuration
        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.LogoUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(x => x.Locale)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("en-US");

        builder.Property(x => x.ContactEmail)
            .HasMaxLength(255);

        builder.Property(x => x.ContactPhone)
            .HasMaxLength(50);

        builder.Property(x => x.LowStockThreshold)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(x => x.WebsiteUrl)
            .HasMaxLength(500);

        // Real json column, same as Product.Metadata: MariaDB adds
        // CHECK (json_valid(...)) so a malformed shape is rejected by the database.
        builder.Property(x => x.MetadataShape)
            .HasColumnName("meta_data_shape")
            .HasColumnType("json");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}
