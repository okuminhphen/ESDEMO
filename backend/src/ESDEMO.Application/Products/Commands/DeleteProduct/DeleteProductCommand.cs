using ESDEMO.Application.Common.Behaviors;
using ESDEMO.Application.Products.Dtos;
using MediatR;

namespace ESDEMO.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id, DeleteProductRequestDto Data) : IRequest, IValidatedRequest
{
    public object Payload => Data;
}
