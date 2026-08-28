using MerchForge.api.Enums;
using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class WebsiteTemplateCustomizableComponentConfiguration
    : IEntityTypeConfiguration<WebsiteTemplateCustomizableComponent>
{
    private static WebsiteTemplateCustomizableComponent Seed(
        string id,
        Guid websiteTemplateId,
        string key,
        string label,
        WebsiteCustomizableValueType valueType,
        int displayOrder,
        string? helpText = null) => new()
        {
            Id = Guid.Parse(id),
            WebsiteTemplateId = websiteTemplateId,
            Key = key,
            Label = label,
            ValueType = valueType,
            IsRequired = false,
            HelpText = helpText,
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedAt = BusinessDomainConfiguration.SeedTimestamp,
            UpdatedAt = BusinessDomainConfiguration.SeedTimestamp,
        };

    public void Configure(EntityTypeBuilder<WebsiteTemplateCustomizableComponent> builder)
    {
        builder.ToTable("website_template_customizable_components");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Label)
            .IsRequired()
            .HasMaxLength(100);

        // Stored as its name, not its ordinal: this value drives type-directed
        // coercion/rendering indefinitely, so an ordinal would silently change
        // meaning if the enum were ever reordered.
        builder.Property(x => x.ValueType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.IsRequired)
            .IsRequired();

        builder.Property(x => x.AllowedValues)
            .HasColumnType("json");

        builder.Property(x => x.HelpText)
            .HasMaxLength(255);

        builder.Property(x => x.DisplayOrder)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Restrict: retiring a template must not delete slot definitions that
        // businesses may have already saved values under.
        builder.HasOne(x => x.WebsiteTemplate)
            .WithMany(x => x.CustomizableComponents)
            .HasForeignKey(x => x.WebsiteTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        // One slot per key per template — a shared frontend component (fashion and
        // grocery templates literally share Hero.tsx today) still gets independent
        // rows per WebsiteTemplateId, never deduped across templates.
        builder.HasIndex(x => new { x.WebsiteTemplateId, x.Key })
            .IsUnique();

        var fashionTemplateId = Guid.Parse("e1000000-0000-4000-8000-000000000001");
        var electronicTemplateId = Guid.Parse("e1000000-0000-4000-8000-000000000002");

        builder.HasData(
            // ---- fashion-template-01 ----
            Seed("f1000000-0000-4000-8000-000000000001", fashionTemplateId, "heroImage", "Hero image", WebsiteCustomizableValueType.Image, 1,
                "Replaces the first hero slide's image. Recommended size ~1920x800px."),
            Seed("f1000000-0000-4000-8000-000000000002", fashionTemplateId, "heroHeadline", "Hero headline", WebsiteCustomizableValueType.Text, 2,
                "Replaces the first hero slide's heading text."),
            Seed("f1000000-0000-4000-8000-000000000003", fashionTemplateId, "promoBannerImage", "Promo banner image", WebsiteCustomizableValueType.Image, 3,
                "Recommended size ~1200x600px."),
            Seed("f1000000-0000-4000-8000-000000000004", fashionTemplateId, "promoBannerText", "Promo banner text", WebsiteCustomizableValueType.Text, 4),

            // ---- electronic-template-01 ----
            Seed("f2000000-0000-4000-8000-000000000001", electronicTemplateId, "heroImage", "Hero image", WebsiteCustomizableValueType.Image, 1,
                "Replaces the first hero slide's image. Recommended size ~1920x800px."),
            Seed("f2000000-0000-4000-8000-000000000002", electronicTemplateId, "heroHeadline", "Hero headline", WebsiteCustomizableValueType.Text, 2,
                "Replaces the first hero slide's heading text."),
            Seed("f2000000-0000-4000-8000-000000000003", electronicTemplateId, "promoBannerImage", "Promo banner image", WebsiteCustomizableValueType.Image, 3,
                "Recommended size ~1200x600px."),
            Seed("f2000000-0000-4000-8000-000000000004", electronicTemplateId, "promoBannerText", "Promo banner text", WebsiteCustomizableValueType.Text, 4));
    }
}
