using System.Globalization;
using ESDEMO.Application.Auth.Abstractions;
using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Orders.Abstractions;
using ESDEMO.Application.Orders.Dtos;
using MediatR;

namespace ESDEMO.Application.Orders.Commands.PayOrder;

public sealed class PayOrderCommandHandler(ICustomerOrderRepository orders, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<PayOrderCommand, OrderResponseDto>
{
    public Task<OrderResponseDto> Handle(PayOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new AuthenticationFailedException();
        var key = request.Data.IdempotencyKey.Trim();
        return orders.PayAsync(userId, request.OrderId, request.Data,
            OrderHash.Create(request.OrderId.ToString("N"), request.Data.Amount.ToString(CultureInfo.InvariantCulture), key),
            clock.GetUtcNow(), cancellationToken);
    }
}
