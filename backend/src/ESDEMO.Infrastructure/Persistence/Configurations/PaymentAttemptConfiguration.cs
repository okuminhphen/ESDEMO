using ESDEMO.Domain.Orders;
using ESDEMO.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ESDEMO.Infrastructure.Persistence.Configurations;

internal sealed class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("PaymentAttempts", table =>
        {
            table.HasCheckConstraint("CK_PaymentAttempts_Amount", "\"EnteredAmount\" >= 0 AND \"EnteredAmount\" <= 999999999999999999 AND \"EnteredAmount\" = trunc(\"EnteredAmount\")");
            table.HasCheckConstraint("CK_PaymentAttempts_Currency", "\"Currency\" = 'VND'");
            table.HasCheckConstraint("CK_PaymentAttempts_Provider", "\"Provider\" = 'Mock'");
            table.HasCheckConstraint("CK_PaymentAttempts_Status", "\"Status\" IN ('Pending', 'Succeeded', 'Failed')");
            table.HasCheckConstraint("CK_PaymentAttempts_Completion", "(\"Status\" = 'Pending' AND \"CompletedAt\" IS NULL AND \"FailureCode\" IS NULL) OR (\"Status\" = 'Succeeded' AND \"CompletedAt\" IS NOT NULL AND \"CompletedAt\" >= \"CreatedAt\" AND \"FailureCode\" IS NULL) OR (\"Status\" = 'Failed' AND \"CompletedAt\" IS NOT NULL AND \"CompletedAt\" >= \"CreatedAt\" AND \"FailureCode\" IS NOT NULL AND length(btrim(\"FailureCode\")) > 0)");
            table.HasCheckConstraint("CK_PaymentAttempts_IdempotencyKey", "length(btrim(\"IdempotencyKey\")) > 0");
            table.HasCheckConstraint("CK_PaymentAttempts_RequestHash", "\"RequestHash\" ~ '^[0-9A-Fa-f]{64}$'");
        });
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(payment => payment.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(payment => payment.EnteredAmount).HasColumnType("numeric");
        builder.Property(payment => payment.Currency).HasMaxLength(3).IsRequired();
        builder.Property(payment => payment.Provider).HasMaxLength(20).IsRequired();
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(payment => payment.FailureCode).HasMaxLength(100);
        builder.HasIndex(payment => new { payment.UserId, payment.IdempotencyKey }).IsUnique();
        builder.HasIndex(payment => payment.OrderId).IsUnique()
            .HasFilter("\"Status\" = 'Succeeded'").HasDatabaseName("UX_PaymentAttempts_OneSuccessPerOrder");
        builder.HasIndex(payment => new { payment.OrderId, payment.CreatedAt });
        // A payment's user must be the owner of the referenced order.
        builder.HasOne<Order>().WithMany()
            .HasForeignKey(payment => new { payment.OrderId, payment.UserId })
            .HasPrincipalKey(order => new { order.Id, order.UserId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("Version").IsRowVersion();
    }
}
