using MerchForge.api.Enums;
using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations
{
    public class BusinessUserRoleConfiguration : IEntityTypeConfiguration<BusinessUserRole>
    {
        public void Configure(EntityTypeBuilder<BusinessUserRole> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Role)
                .IsRequired()
                 .HasConversion<string>()
                .HasMaxLength(50);

            builder.HasData(
                new BusinessUserRole
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Role = BusinessRole.Owner
                },
                new BusinessUserRole
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Role = BusinessRole.Admin
                },
                new BusinessUserRole
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Role = BusinessRole.Member
                }
            );
        }
    }
}
