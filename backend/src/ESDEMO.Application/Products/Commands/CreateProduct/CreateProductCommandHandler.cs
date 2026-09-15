using ESDEMO.Application.Products.Abstractions;
using ESDEMO.Application.Products.Dtos;
using ESDEMO.Domain.Products;
using MediatR;

namespace ESDEMO.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(IProductRepository products, TimeProvider clock)
    : IRequestHandler<CreateProductCommand, ProductResponseDto>
{
    public Task<ProductResponseDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;
        var product = new Product
        {
            Sku = data.Sku.Trim().ToUpperInvariant(),
            Name = data.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim(),
            Price = data.Price!.Value,
            StockQuantity = data.StockQuantity!.Value,
            IsActive = data.IsActive!.Value,
            CreatedAt = clock.GetUtcNow()
        };
        return products.AddAsync(product, cancellationToken);
    }
}
