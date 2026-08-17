using MerchForge.api.Enums;
using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Data.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Role)
                .IsRequired()
                 .HasConversion<string>()
                .HasMaxLength(50);

            builder.HasData(
                new UserRole
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Role = SystemRole.SuperAdmin
                },
                new UserRole
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Role = SystemRole.Admin
                },
                new UserRole
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Role = SystemRole.User
                }
            );
        }
    }
}