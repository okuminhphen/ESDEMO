using ESDEMO.Application.Common.Behaviors;
using ESDEMO.Application.Products.Dtos;
using MediatR;

namespace ESDEMO.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(Guid Id, UpdateProductRequestDto Data) : IRequest<ProductResponseDto>, IValidatedRequest
{
    public object Payload => Data;
}
