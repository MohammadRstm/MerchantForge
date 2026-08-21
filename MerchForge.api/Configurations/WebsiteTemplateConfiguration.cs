using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class WebsiteTemplateConfiguration : IEntityTypeConfiguration<WebsiteTemplate>
{
    private static WebsiteTemplate Seed(
        string id,
        Guid domainId,
        string name,
        string label,
        string videoPreviewUrl,
        int displayOrder) => new()
        {
            Id = Guid.Parse(id),
            BusinessDomainId = domainId,
            Name = name,
            Label = label,
            VideoPreviewUrl = videoPreviewUrl,
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
            UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
        };

    public void Configure(EntityTypeBuilder<WebsiteTemplate> builder)
    {
        builder.ToTable("website_templates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Label)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.VideoPreviewUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.DisplayOrder)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Global, not per-domain: Name is how a deployment step later identifies which
        // physical template project a business chose, so two domains reusing the same
        // name would be ambiguous, not just visually confusing.
        builder.HasIndex(x => x.Name)
            .IsUnique();

        // Restrict, not Cascade: retiring a domain must not silently delete the
        // templates businesses in it have already chosen.
        builder.HasOne(x => x.BusinessDomain)
            .WithMany()
            .HasForeignKey(x => x.BusinessDomainId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            Seed("e1000000-0000-4000-8000-000000000001", BusinessDomainConfiguration.FashionId,
                "fashion-template-01", "Vineta Fashion", "/videos/templates/coming-soon.mp4", 1),
            Seed("e1000000-0000-4000-8000-000000000002", BusinessDomainConfiguration.ElectronicsId,
                "electronic-template-01", "Vineta Electronics", "/videos/templates/coming-soon.mp4", 1));
    }
}
