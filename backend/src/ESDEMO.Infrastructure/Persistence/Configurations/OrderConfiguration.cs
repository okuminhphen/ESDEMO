using ESDEMO.Domain.Orders;
using ESDEMO.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ESDEMO.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_TotalAmount", "\"TotalAmount\" >= 0 AND \"TotalAmount\" <= 999999999999999999 AND \"TotalAmount\" = trunc(\"TotalAmount\")");
            table.HasCheckConstraint("CK_Orders_Currency", "\"Currency\" = 'VND'");
            table.HasCheckConstraint("CK_Orders_Status", "\"Status\" IN ('PendingPayment', 'Paid', 'Cancelled', 'Expired')");
            table.HasCheckConstraint("CK_Orders_Expiry", "\"ExpiresAt\" > \"CreatedAt\"");
            table.HasCheckConstraint("CK_Orders_PaidAt", "(\"Status\" = 'Paid' AND \"PaidAt\" IS NOT NULL AND \"PaidAt\" >= \"CreatedAt\" AND \"PaidAt\" <= \"ExpiresAt\") OR (\"Status\" <> 'Paid' AND \"PaidAt\" IS NULL)");
            table.HasCheckConstraint("CK_Orders_Number", "length(btrim(\"OrderNumber\")) > 0");
            table.HasCheckConstraint("CK_Orders_IdempotencyKey", "length(btrim(\"IdempotencyKey\")) > 0");
            table.HasCheckConstraint("CK_Orders_RequestHash", "\"RequestHash\" ~ '^[0-9A-Fa-f]{64}$'");
        });
        builder.HasKey(order => order.Id);
        builder.HasAlternateKey(order => new { order.Id, order.UserId });
        builder.Property(order => order.OrderNumber).HasMaxLength(32).IsRequired();
        builder.Property(order => order.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(order => order.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(order => order.Currency).HasMaxLength(3).IsRequired();
        builder.Property(order => order.TotalAmount).HasColumnType("numeric");
        builder.HasIndex(order => order.OrderNumber).IsUnique();
        builder.HasIndex(order => new { order.UserId, order.IdempotencyKey }).IsUnique();
        builder.HasIndex(order => new { order.UserId, order.CreatedAt });
        builder.HasIndex(order => new { order.Status, order.ExpiresAt });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(order => order.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(order => order.Items).WithOne().HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("Version").IsRowVersion();
    }
}
