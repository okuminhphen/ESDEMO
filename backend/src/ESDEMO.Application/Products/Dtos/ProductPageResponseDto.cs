namespace ESDEMO.Application.Products.Dtos;

public sealed record ProductPageResponseDto(
    IReadOnlyList<ProductResponseDto> Items, int TotalCount, int Page, int PageSize);
