using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", t =>
        {
            t.HasCheckConstraint("CK_orders_Subtotal_NonNegative", "`Subtotal` >= 0");
            t.HasCheckConstraint("CK_orders_Total_NonNegative", "`Total` >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.CustomerEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.CustomerPhone)
            .HasMaxLength(50);

        builder.Property(x => x.ShippingAddressLine1)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ShippingAddressLine2)
            .HasMaxLength(255);

        builder.Property(x => x.ShippingCity)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ShippingState)
            .HasMaxLength(100);

        builder.Property(x => x.ShippingPostalCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.ShippingCountry)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CustomerNotes)
            .HasMaxLength(1000);

        // Stored by name, not ordinal — same reasoning as every other enum column in
        // this codebase (ProductAttributeDefinition.ValueType, WebsiteTemplateRequest.Status):
        // an order's status is read back and compared long after the enum could have
        // been reordered.
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.PaymentStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Subtotal)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.Total)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Deleting a business removes its own order history too — same reasoning as
        // Product -> Business (Cascade): this is genuinely owned data, not shared
        // reference data.
        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        // A customer deleting their account must never take a business's order history
        // with it — every other field on Order already snapshots what it needs, so
        // SetNull just detaches the link rather than cascading.
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.BusinessId);

        builder.HasIndex(x => x.CustomerId);

        // The dashboard's order list is always business-scoped and usually
        // status-filtered, newest first.
        builder.HasIndex(x => new { x.BusinessId, x.Status, x.CreatedAt });
    }
}
