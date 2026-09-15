using ESDEMO.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ESDEMO.Infrastructure.Persistence.Configurations;

internal sealed class RefreshSessionConfiguration : IEntityTypeConfiguration<RefreshSession>
{
    public void Configure(EntityTypeBuilder<RefreshSession> builder)
    {
        builder.ToTable("RefreshSessions", table =>
        {
            table.HasCheckConstraint("CK_RefreshSessions_TokenHash", "\"TokenHash\" ~ '^[0-9A-Fa-f]{64}$'");
            table.HasCheckConstraint("CK_RefreshSessions_Expiry", "\"ExpiresAt\" > \"CreatedAt\"");
            table.HasCheckConstraint("CK_RefreshSessions_RevokedAt", "\"RevokedAt\" IS NULL OR \"RevokedAt\" >= \"CreatedAt\"");
            table.HasCheckConstraint("CK_RefreshSessions_Replacement", "\"ReplacedById\" IS NULL OR (\"ReplacedById\" <> \"Id\" AND \"RevokedAt\" IS NOT NULL)");
        });
        builder.HasKey(session => session.Id);
        builder.HasAlternateKey(session => new { session.Id, session.UserId, session.FamilyId });
        builder.Property(session => session.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(session => session.TokenHash).IsUnique();
        builder.HasIndex(session => new { session.UserId, session.FamilyId });
        builder.HasIndex(session => session.ExpiresAt);
        builder.HasIndex(session => session.ReplacedById).IsUnique().HasFilter("\"ReplacedById\" IS NOT NULL");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(session => session.UserId).OnDelete(DeleteBehavior.Restrict);
        // A rotated token must stay within the same user and token family.
        builder.HasOne<RefreshSession>().WithMany()
            .HasForeignKey(session => new { session.ReplacedById, session.UserId, session.FamilyId })
            .HasPrincipalKey(session => new { session.Id, session.UserId, session.FamilyId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("Version").IsRowVersion();
    }
}
