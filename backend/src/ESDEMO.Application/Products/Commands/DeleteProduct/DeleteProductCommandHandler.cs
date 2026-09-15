using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Products.Abstractions;
using MediatR;

namespace ESDEMO.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(IProductRepository products, TimeProvider clock)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.FindAsync(request.Id, cancellationToken);
        if (product is null || product.DeletedAt is not null)
        {
            throw new NotFoundException("Product was not found or has been deleted.");
        }

        var now = clock.GetUtcNow();
        product.IsActive = false;
        product.DeletedAt = now;
        product.UpdatedAt = now;
        await products.UpdateAsync(product, request.Data.Version!.Value, cancellationToken);
    }
}
