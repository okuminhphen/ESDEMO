using ESDEMO.Application.Auth.Abstractions;
using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Orders.Abstractions;
using ESDEMO.Application.Orders.Dtos;
using MediatR;

namespace ESDEMO.Application.Orders.Queries.GetMyOrder;

public sealed record GetMyOrderQuery(Guid OrderId) : IRequest<OrderResponseDto>;

public sealed class GetMyOrderQueryHandler(ICustomerOrderRepository orders, ICurrentUser currentUser)
    : IRequestHandler<GetMyOrderQuery, OrderResponseDto>
{
    public async Task<OrderResponseDto> Handle(GetMyOrderQuery request, CancellationToken cancellationToken) =>
        await orders.GetAsync(currentUser.UserId ?? throw new AuthenticationFailedException(), request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
}
