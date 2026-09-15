using ESDEMO.Application.Products.Abstractions;
using ESDEMO.Application.Products.Dtos;
using MediatR;

namespace ESDEMO.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IProductRepository products) : IRequestHandler<GetProductsQuery, ProductPageResponseDto>
{
    public Task<ProductPageResponseDto> Handle(GetProductsQuery request, CancellationToken cancellationToken) =>
        products.GetPageAsync(request.Data, cancellationToken);
}
