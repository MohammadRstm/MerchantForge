using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Data.Configurations;

public class InvitationConfiguration
    : IEntityTypeConfiguration<Invitation>
{
    public void Configure(
        EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(i => i.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(i => i.Type)
            .IsRequired();

        builder.Property(i => i.BusinessRole)
            .IsRequired(false);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.ExpiresAt)
            .IsRequired();

        builder.HasOne(i => i.Business)
            .WithMany()
            .HasForeignKey(i => i.BusinessId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.TokenHash)
            .IsUnique();

        builder.HasIndex(i => i.Email);
    }
}