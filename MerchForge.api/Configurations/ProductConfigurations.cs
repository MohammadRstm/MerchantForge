using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MerchForge.api.Models;

namespace MerchForge.api.Configurations;

public class ProductConfiguration
    : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.Price)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasMaxLength(100);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(500);

        builder.HasOne(x => x.Business)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.BusinessId);
    }
}