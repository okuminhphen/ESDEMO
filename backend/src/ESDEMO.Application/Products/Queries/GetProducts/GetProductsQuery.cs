using ESDEMO.Application.Common.Behaviors;
using ESDEMO.Application.Products.Dtos;
using MediatR;

namespace ESDEMO.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery(ProductListRequestDto Data) : IRequest<ProductPageResponseDto>, IValidatedRequest
{
    public object Payload => Data;
}
