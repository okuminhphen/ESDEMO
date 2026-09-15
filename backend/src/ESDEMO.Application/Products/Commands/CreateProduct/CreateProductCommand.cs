using ESDEMO.Application.Common.Behaviors;
using ESDEMO.Application.Products.Dtos;
using MediatR;

namespace ESDEMO.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(CreateProductRequestDto Data) : IRequest<ProductResponseDto>, IValidatedRequest
{
    public object Payload => Data;
}
