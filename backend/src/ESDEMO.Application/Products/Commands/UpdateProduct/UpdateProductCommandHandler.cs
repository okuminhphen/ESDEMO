using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Products.Abstractions;
using ESDEMO.Application.Products.Dtos;
using MediatR;

namespace ESDEMO.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(IProductRepository products, TimeProvider clock)
    : IRequestHandler<UpdateProductCommand, ProductResponseDto>
{
    public async Task<ProductResponseDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.FindAsync(request.Id, cancellationToken);
        if (product is null || product.DeletedAt is not null)
        {
            throw new NotFoundException("Product was not found or has been deleted.");
        }

        var data = request.Data;
        product.Sku = data.Sku.Trim().ToUpperInvariant();
        product.Name = data.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim();
        product.Price = data.Price!.Value;
        product.StockQuantity = data.StockQuantity!.Value;
        product.IsActive = data.IsActive!.Value;
        product.UpdatedAt = clock.GetUtcNow();
        return await products.UpdateAsync(product, data.Version!.Value, cancellationToken);
    }
}
