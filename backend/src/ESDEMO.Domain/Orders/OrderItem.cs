namespace ESDEMO.Domain.Orders;

public sealed class OrderItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public required string ProductNameSnapshot { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
}
