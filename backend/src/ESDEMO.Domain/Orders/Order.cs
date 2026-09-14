namespace ESDEMO.Domain.Orders;

public sealed class Order
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string OrderNumber { get; init; }
    public Guid UserId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string RequestHash { get; init; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "VND";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? PaidAt { get; set; }
    public ICollection<OrderItem> Items { get; init; } = new List<OrderItem>();
}
