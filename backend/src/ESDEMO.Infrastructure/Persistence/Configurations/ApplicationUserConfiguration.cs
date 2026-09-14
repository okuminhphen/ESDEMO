using ESDEMO.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ESDEMO.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AspNetUsers", table =>
        {
            table.HasCheckConstraint("CK_AspNetUsers_DisplayName", "length(btrim(\"DisplayName\")) > 0");
            table.HasCheckConstraint("CK_AspNetUsers_Email", "length(btrim(\"Email\")) > 0 AND length(btrim(\"NormalizedEmail\")) > 0");
        });
        builder.Property(user => user.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(user => user.UserName).HasMaxLength(256).IsRequired();
        builder.Property(user => user.NormalizedUserName).HasMaxLength(256).IsRequired();
        builder.HasIndex(user => user.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
    }
}
