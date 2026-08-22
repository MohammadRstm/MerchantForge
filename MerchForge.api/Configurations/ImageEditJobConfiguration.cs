using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class ImageEditJobConfiguration
    : IEntityTypeConfiguration<ImageEditJob>
{
    public void Configure(EntityTypeBuilder<ImageEditJob> builder)
    {
        builder.ToTable("image_edit_jobs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessId)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId)
            .IsRequired();

        builder.Property(x => x.Prompt)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.InputImageUrls)
            .IsRequired()
            .HasColumnType("json");

        builder.Property(x => x.OutputImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => x.BusinessId);

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
