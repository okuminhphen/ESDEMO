namespace ESDEMO.Domain.Notifications;

public sealed class Notification
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public Guid OrderId { get; init; }
    public Guid SourceEventId { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}
