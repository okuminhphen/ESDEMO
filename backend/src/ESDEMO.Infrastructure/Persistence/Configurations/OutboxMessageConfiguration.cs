using ESDEMO.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ESDEMO.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", table =>
        {
            table.HasCheckConstraint("CK_OutboxMessages_EventType", "length(btrim(\"EventType\")) > 0");
            table.HasCheckConstraint("CK_OutboxMessages_Payload", "jsonb_typeof(\"Payload\") = 'object'");
            table.HasCheckConstraint("CK_OutboxMessages_RetryCount", "\"RetryCount\" >= 0");
            table.HasCheckConstraint("CK_OutboxMessages_ProcessedAt", "\"ProcessedAt\" IS NULL OR \"ProcessedAt\" >= \"OccurredAt\"");
        });
        builder.HasKey(message => message.Id);
        builder.Property(message => message.EventType).HasMaxLength(200).IsRequired();
        builder.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(2000);
        builder.HasIndex(message => new { message.ProcessedAt, message.LeaseExpiresAt, message.NextAttemptAt, message.OccurredAt });
        builder.HasIndex(message => new { message.NextAttemptAt, message.OccurredAt })
            .HasFilter("\"ProcessedAt\" IS NULL");
        builder.Property<uint>("Version").IsRowVersion();
    }
}
