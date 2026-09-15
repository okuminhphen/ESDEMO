using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Products.Abstractions;
using ESDEMO.Application.Products.Dtos;
using MediatR;

namespace ESDEMO.Application.Products.Queries.GetProduct;

public sealed class GetProductQueryHandler(IProductRepository products) : IRequestHandler<GetProductQuery, ProductResponseDto>
{
    public async Task<ProductResponseDto> Handle(GetProductQuery request, CancellationToken cancellationToken) =>
        await products.GetDetailsAsync(request.Id, cancellationToken)
        ?? throw new NotFoundException("Product was not found.");
}
