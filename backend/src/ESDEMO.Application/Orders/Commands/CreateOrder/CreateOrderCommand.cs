using ESDEMO.Application.Common.Behaviors;
using ESDEMO.Application.Orders.Dtos;
using MediatR;

namespace ESDEMO.Application.Orders.Commands.CreateOrder;

public sealed record CreateOrderCommand(CreateOrderRequestDto Data) : IRequest<OrderResponseDto>, IValidatedRequest
{
    public object Payload => Data;
}
