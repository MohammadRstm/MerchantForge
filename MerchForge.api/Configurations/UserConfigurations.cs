using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MerchForge.api.Models;

namespace MerchForge.api.Configurations;

public class UserConfigurations : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.Property(x => x.PasswordHash)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => x.DisabledAt);

        // Self-referencing FK; Restrict rather than Cascade/SetNull since users are
        // never hard-deleted (no delete path exists), so this never actually fires -
        // Restrict simply keeps that failure loud instead of silently nulling data.
        builder.HasOne(x => x.DisabledByUser)
            .WithMany()
            .HasForeignKey(x => x.DisabledByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}