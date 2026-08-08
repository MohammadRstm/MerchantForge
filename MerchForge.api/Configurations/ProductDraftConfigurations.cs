using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MerchForge.api.Models;

namespace MerchForge.api.Configurations;

public class ProductDraftConfiguration
    : IEntityTypeConfiguration<ProductDraft>
{
    public void Configure(EntityTypeBuilder<ProductDraft> builder)
    {
        builder.ToTable("product_drafts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ConversationId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(x => x.Business)
            .WithMany(x => x.ProductDrafts)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.BusinessId,
            x.Provider,
            x.ConversationId
        });
    }
}