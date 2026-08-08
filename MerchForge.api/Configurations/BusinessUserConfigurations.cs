using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MerchForge.api.Models;

namespace MerchForge.api.Configurations;

public class BusinessUserConfiguration
    : IEntityTypeConfiguration<BusinessUser>
{
    public void Configure(EntityTypeBuilder<BusinessUser> builder)
    {
        builder.ToTable("business_users");

        builder.HasKey(x => new
        {
            x.BusinessId,
            x.UserId
        });

        builder.HasOne(x => x.Business)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.BusinessMemberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
    }
}