using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class WebsiteTemplateRequestConfiguration : IEntityTypeConfiguration<WebsiteTemplateRequest>
{
    public void Configure(EntityTypeBuilder<WebsiteTemplateRequest> builder)
    {
        builder.ToTable("website_template_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessId)
            .IsRequired();

        builder.Property(x => x.RequestedByUserId)
            .IsRequired();

        builder.Property(x => x.WebsiteTemplateId)
            .IsRequired();

        builder.Property(x => x.CustomizationNotes)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.FinalWebsiteUrl)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.BusinessId);

        builder.HasIndex(x => x.WebsiteTemplateId);

        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Business)
            .WithMany(b => b.WebsiteTemplateRequests)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade: retiring a template must not silently delete the
        // request history built against it.
        builder.HasOne(x => x.WebsiteTemplate)
            .WithMany()
            .HasForeignKey(x => x.WebsiteTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
