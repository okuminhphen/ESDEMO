using ESDEMO.Application.Auth.Abstractions;
using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Orders.Abstractions;
using ESDEMO.Application.Orders.Dtos;
using MediatR;

namespace ESDEMO.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler(ICustomerOrderRepository orders, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<CreateOrderCommand, OrderResponseDto>
{
    public Task<OrderResponseDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new AuthenticationFailedException();
        var key = request.Data.IdempotencyKey.Trim();
        return orders.CreateOrGetAsync(userId, request.Data,
            OrderHash.Create(request.Data.ProductId.ToString("N"), key), clock.GetUtcNow(), cancellationToken);
    }
}
