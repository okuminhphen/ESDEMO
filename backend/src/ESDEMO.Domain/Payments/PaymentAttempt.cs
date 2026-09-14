namespace ESDEMO.Domain.Payments;

public sealed class PaymentAttempt
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string RequestHash { get; init; }
    public decimal EnteredAmount { get; init; }
    public string Currency { get; init; } = "VND";
    public string Provider { get; init; } = "Mock";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? FailureCode { get; set; }
}
