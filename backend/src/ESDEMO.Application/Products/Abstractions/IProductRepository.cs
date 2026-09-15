using ESDEMO.Application.Products.Dtos;
using ESDEMO.Domain.Products;

namespace ESDEMO.Application.Products.Abstractions;

// Feature-specific persistence; no IQueryable or EF types cross the layer boundary.
public interface IProductRepository
{
    Task<Product?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductResponseDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductPageResponseDto> GetPageAsync(ProductListRequestDto request, CancellationToken cancellationToken);
    Task<ProductResponseDto> AddAsync(Product product, CancellationToken cancellationToken);
    Task<ProductResponseDto> UpdateAsync(Product product, uint expectedVersion, CancellationToken cancellationToken);
}
