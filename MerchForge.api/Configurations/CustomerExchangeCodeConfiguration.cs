using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MerchForge.api.Models;

namespace MerchForge.api.Configurations;

public class CustomerExchangeCodeConfiguration
    : IEntityTypeConfiguration<CustomerExchangeCode>
{
    public void Configure(EntityTypeBuilder<CustomerExchangeCode> builder)
    {
        builder.ToTable("customer_exchange_codes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodeHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(x => x.CodeHash)
            .IsUnique();

        builder.Property(x => x.ReturnUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CustomerId);
    }
}
