namespace ESDEMO.Application.Products.Dtos;

public sealed record ProductResponseDto(
    Guid Id, string Sku, string Name, string? Description, decimal Price,
    int StockQuantity, bool IsActive, DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt, DateTimeOffset? DeletedAt, uint Version);
