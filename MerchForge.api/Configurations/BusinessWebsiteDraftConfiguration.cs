using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MerchForge.api.Models;

namespace MerchForge.api.Configurations;

public class BusinessWebsiteDraftConfiguration : IEntityTypeConfiguration<BusinessWebsiteDraft>
{
    public void Configure(EntityTypeBuilder<BusinessWebsiteDraft> builder)
    {
        builder.ToTable("business_website_drafts");

        // Shared-key 1:1: BusinessId is both PK and FK, same cascade-ownership
        // convention already used for Order -> Business (this is genuinely owned,
        // dependent data, not shared reference data).
        builder.HasKey(x => x.BusinessId);

        builder.HasOne(x => x.Business)
            .WithOne(x => x.WebsiteDraft)
            .HasForeignKey<BusinessWebsiteDraft>(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Tagline).HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.LogoUrl).HasMaxLength(500);
        builder.Property(x => x.FaviconUrl).HasMaxLength(500);
        builder.Property(x => x.ContactEmail).HasMaxLength(255);
        builder.Property(x => x.ContactPhone).HasMaxLength(50);
        builder.Property(x => x.WhatsAppNumber).HasMaxLength(50);
        builder.Property(x => x.AddressLine1).HasMaxLength(255);
        builder.Property(x => x.AddressLine2).HasMaxLength(255);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.State).HasMaxLength(100);
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.PrimaryColor).HasMaxLength(7);

        builder.Property(x => x.SocialLinks).HasColumnType("json");
        builder.Property(x => x.BusinessHours).HasColumnType("json");
        builder.Property(x => x.TemplateFieldsDraft).HasColumnType("json");

        builder.Property(x => x.PreviewToken)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(x => x.PreviewToken)
            .IsUnique();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}
