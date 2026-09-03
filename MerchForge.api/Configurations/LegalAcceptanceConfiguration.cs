using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerchForge.api.Configurations;

public class LegalAcceptanceConfiguration : IEntityTypeConfiguration<LegalAcceptance>
{
    public void Configure(EntityTypeBuilder<LegalAcceptance> builder)
    {
        builder.ToTable("legal_acceptances", t =>
        {
            // A row is either a User's acceptance or a Customer's, never both and
            // never neither — the same guarantee the model's doc comment describes,
            // enforced in the database rather than trusted to every future caller.
            t.HasCheckConstraint(
                "CK_legal_acceptances_ExactlyOneOwner",
                "(`UserId` IS NULL) <> (`CustomerId` IS NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TermsVersion)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.PrivacyPolicyVersion)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.AcceptedAt)
            .IsRequired();

        // Cascade on both: this row only ever means something in the context of the
        // account it belongs to, same as RefreshToken/CustomerRefreshToken.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CustomerId);
    }
}
