using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_history");

        builder.HasKey(x => x.Id);

        // Stored by name, not ordinal — same reasoning as Order.Status.
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.ChangedByUserId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Owned data — deleting the order deletes its history with it, same reasoning
        // as OrderItem -> Order.
        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.OrderId, x.CreatedAt });
    }
}
