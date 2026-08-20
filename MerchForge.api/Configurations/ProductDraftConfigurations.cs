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

        // Stored as its name, like every other enum that reaches the database here:
        // an ordinal would silently change meaning if a state were ever inserted
        // into the middle of the lifecycle.
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        // Real json columns, same as Product.Metadata — MariaDB adds
        // CHECK (json_valid(...)) so malformed state is rejected by the database.
        builder.Property(x => x.Draft)
            .HasColumnType("json");

        builder.Property(x => x.Messages)
            .HasColumnType("json");

        builder.Property(x => x.OriginalImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ProcessedImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ImageModificationPrompt)
            .HasMaxLength(1000);

        // Nullable: a dashboard-initiated draft has no external conversation.
        builder.Property(x => x.Provider)
            .HasMaxLength(50);

        builder.Property(x => x.ConversationId)
            .HasMaxLength(255);

        builder.HasOne(x => x.Business)
            .WithMany(x => x.ProductDrafts)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        // SetNull, not Cascade: deleting a product the owner later changed their mind
        // about must not erase the conversation that produced it.
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // Drafts are always listed and looked up per business.
        builder.HasIndex(x => new { x.BusinessId, x.Status });

        builder.HasIndex(x => new
        {
            x.BusinessId,
            x.Provider,
            x.ConversationId
        });
    }
}
