using ESDEMO.Application.Products.Dtos;
using MediatR;

namespace ESDEMO.Application.Products.Queries.GetProduct;

public sealed record GetProductQuery(Guid Id) : IRequest<ProductResponseDto>;
