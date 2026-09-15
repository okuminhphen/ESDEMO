namespace ESDEMO.Application.Products.Dtos;

public sealed record PublicProductResponseDto(
    Guid Id, string Sku, string Name, string? Description, decimal Price, bool IsInStock);

public sealed record PublicProductPageResponseDto(
    IReadOnlyList<PublicProductResponseDto> Items, int TotalCount, int Page, int PageSize);
