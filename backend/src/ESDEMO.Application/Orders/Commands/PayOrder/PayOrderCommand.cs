using ESDEMO.Application.Common.Behaviors;
using ESDEMO.Application.Orders.Dtos;
using MediatR;

namespace ESDEMO.Application.Orders.Commands.PayOrder;

public sealed record PayOrderCommand(Guid OrderId, PayOrderRequestDto Data) : IRequest<OrderResponseDto>, IValidatedRequest
{
    public object Payload => Data;
}
