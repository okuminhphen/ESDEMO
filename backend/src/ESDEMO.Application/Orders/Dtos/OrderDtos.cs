using System.ComponentModel.DataAnnotations;

namespace ESDEMO.Application.Orders.Dtos;

public sealed class CreateOrderRequestDto : IValidatableObject
{
    [Required]
    public Guid ProductId { get; init; }

    [Required, StringLength(100, MinimumLength = 16)]
    public string IdempotencyKey { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IdempotencyKey.Any(char.IsControl))
        {
            yield return new ValidationResult("IdempotencyKey cannot contain control characters.", [nameof(IdempotencyKey)]);
        }
    }
}

public sealed class PayOrderRequestDto : IValidatableObject
{
    [Range(typeof(decimal), "0", "999999999999999999")]
    public decimal Amount { get; init; }

    [Required, StringLength(100, MinimumLength = 16)]
    public string IdempotencyKey { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (decimal.Truncate(Amount) != Amount)
        {
            yield return new ValidationResult("VND amount must be a whole number.", [nameof(Amount)]);
        }
        if (IdempotencyKey.Any(char.IsControl))
        {
            yield return new ValidationResult("IdempotencyKey cannot contain control characters.", [nameof(IdempotencyKey)]);
        }
    }
}

public sealed class OrderListRequestDto : IValidatableObject
{
    [Range(1, 1_000_000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) { yield break; }
}

public sealed record OrderItemResponseDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public sealed record OrderResponseDto(Guid Id, string OrderNumber, string Status, decimal TotalAmount, string Currency,
    DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, DateTimeOffset? PaidAt, IReadOnlyList<OrderItemResponseDto> Items);

public sealed record OrderPageResponseDto(IReadOnlyList<OrderResponseDto> Items, int TotalCount, int Page, int PageSize);
