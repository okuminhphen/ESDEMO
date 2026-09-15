using ESDEMO.Application.Auth.Abstractions;
using ESDEMO.Application.Common.Behaviors;
using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Orders.Abstractions;
using ESDEMO.Application.Orders.Dtos;
using MediatR;

namespace ESDEMO.Application.Orders.Queries.GetMyOrders;

public sealed record GetMyOrdersQuery(OrderListRequestDto Data) : IRequest<OrderPageResponseDto>, IValidatedRequest
{
    public object Payload => Data;
}

public sealed class GetMyOrdersQueryHandler(ICustomerOrderRepository orders, ICurrentUser currentUser)
    : IRequestHandler<GetMyOrdersQuery, OrderPageResponseDto>
{
    public Task<OrderPageResponseDto> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken) =>
        orders.GetPageAsync(currentUser.UserId ?? throw new AuthenticationFailedException(), request.Data, cancellationToken);
}
