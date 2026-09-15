using ESDEMO.Domain.Notifications;
using ESDEMO.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ESDEMO.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", table =>
        {
            table.HasCheckConstraint("CK_Notifications_Title", "length(btrim(\"Title\")) > 0");
            table.HasCheckConstraint("CK_Notifications_Message", "length(btrim(\"Message\")) > 0");
            table.HasCheckConstraint("CK_Notifications_ReadAt", "\"ReadAt\" IS NULL OR \"ReadAt\" >= \"CreatedAt\"");
        });
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Title).HasMaxLength(200).IsRequired();
        builder.Property(notification => notification.Message).HasMaxLength(2000).IsRequired();
        // No FK to Outbox: processed outbox messages may be archived independently.
        builder.HasIndex(notification => notification.SourceEventId).IsUnique();
        builder.HasIndex(notification => new { notification.UserId, notification.CreatedAt });
        builder.HasOne<Order>().WithMany()
            .HasForeignKey(notification => new { notification.OrderId, notification.UserId })
            .HasPrincipalKey(order => new { order.Id, order.UserId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
