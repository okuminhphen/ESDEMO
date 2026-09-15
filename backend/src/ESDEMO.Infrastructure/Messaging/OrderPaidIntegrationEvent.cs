namespace ESDEMO.Infrastructure.Messaging;

public sealed record OrderPaidIntegrationEvent(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    string OrderNumber,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset OccurredAt);
