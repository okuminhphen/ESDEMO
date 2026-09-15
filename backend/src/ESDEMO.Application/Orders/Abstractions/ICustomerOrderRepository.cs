using ESDEMO.Application.Orders.Dtos;

namespace ESDEMO.Application.Orders.Abstractions;

public interface ICustomerOrderRepository
{
    Task<OrderResponseDto> CreateOrGetAsync(Guid userId, CreateOrderRequestDto request, string requestHash,
        DateTimeOffset now, CancellationToken cancellationToken);
    Task<OrderPageResponseDto> GetPageAsync(Guid userId, OrderListRequestDto request, CancellationToken cancellationToken);
    Task<OrderResponseDto?> GetAsync(Guid userId, Guid orderId, CancellationToken cancellationToken);
    Task<OrderResponseDto> PayAsync(Guid userId, Guid orderId, PayOrderRequestDto request, string requestHash,
        DateTimeOffset now, CancellationToken cancellationToken);
}
