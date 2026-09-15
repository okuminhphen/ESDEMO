using System.Linq.Expressions;
using System.Text.Json;
using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Orders.Abstractions;
using ESDEMO.Application.Orders.Dtos;
using ESDEMO.Domain.Orders;
using ESDEMO.Domain.Payments;
using ESDEMO.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ESDEMO.Infrastructure.Persistence.Repositories;

public sealed class CustomerOrderRepository(ApplicationDbContext db) : ICustomerOrderRepository
{
    private static readonly Expression<Func<Order, OrderResponseDto>> Projection = order => new(
        order.Id, order.OrderNumber, order.Status.ToString(), order.TotalAmount, order.Currency,
        order.CreatedAt, order.ExpiresAt, order.PaidAt,
        order.Items.OrderBy(item => item.Id).Select(item => new OrderItemResponseDto(
            item.ProductId, item.ProductNameSnapshot, item.UnitPrice, item.Quantity)).ToList());

    public async Task<OrderResponseDto> CreateOrGetAsync(Guid userId, CreateOrderRequestDto request, string requestHash,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        var key = request.IdempotencyKey.Trim();
        var existing = await db.Orders.AsNoTracking().Where(order => order.UserId == userId && order.IdempotencyKey == key)
            .Select(Projection).SingleOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            await EnsureHashAsync(userId, key, requestHash, cancellationToken);
            return existing;
        }

        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(product => product.Id == request.ProductId
            && product.DeletedAt == null && product.IsActive, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        var expiresAt = now.AddMinutes(30);
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            OrderNumber = $"ORD-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32],
            UserId = userId,
            IdempotencyKey = key,
            RequestHash = requestHash,
            TotalAmount = product.Price,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            Items = [new OrderItem
            {
                OrderId = orderId,
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                UnitPrice = product.Price,
                Quantity = 1
            }]
        };        db.Orders.Add(order);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Orders_UserId_IdempotencyKey" })
        {
            db.ChangeTracker.Clear();
            var raced = await db.Orders.AsNoTracking().Where(item => item.UserId == userId && item.IdempotencyKey == key)
                .Select(Projection).SingleAsync(cancellationToken);
            await EnsureHashAsync(userId, key, requestHash, cancellationToken);
            return raced;
        }
        return ToResponse(order);
    }

    public async Task<OrderPageResponseDto> GetPageAsync(Guid userId, OrderListRequestDto request, CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking().Where(order => order.UserId == userId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(order => order.CreatedAt).ThenByDescending(order => order.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(Projection).ToListAsync(cancellationToken);
        return new OrderPageResponseDto(items, count, request.Page, request.PageSize);
    }

    public Task<OrderResponseDto?> GetAsync(Guid userId, Guid orderId, CancellationToken cancellationToken) =>
        db.Orders.AsNoTracking().Where(order => order.Id == orderId && order.UserId == userId).Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<OrderResponseDto> PayAsync(Guid userId, Guid orderId, PayOrderRequestDto request, string requestHash,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        var key = request.IdempotencyKey.Trim();
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var priorAttempt = await db.PaymentAttempts.AsNoTracking()
                .SingleOrDefaultAsync(payment => payment.UserId == userId && payment.IdempotencyKey == key, cancellationToken);
            if (priorAttempt is not null)
            {
                if (!string.Equals(priorAttempt.RequestHash, requestHash, StringComparison.Ordinal))
                {
                    throw new ConflictException("IdempotencyKey was already used with a different request.");
                }
                var previous = await GetAsync(userId, orderId, cancellationToken) ?? throw new NotFoundException("Order was not found.");
                await transaction.CommitAsync(cancellationToken);
                return previous;
            }

            var order = await db.Orders.Include(item => item.Items).SingleOrDefaultAsync(item => item.Id == orderId && item.UserId == userId, cancellationToken)
                ?? throw new NotFoundException("Order was not found.");
            if (order.Status == OrderStatus.Paid)
            {
                throw new ConflictException("This order has already been paid.");
            }
            if (order.Status != OrderStatus.PendingPayment)
            {
                throw new ConflictException("This order is no longer available for payment.");
            }
            if (order.ExpiresAt <= now)
            {
                order.Status = OrderStatus.Expired;
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                throw new ConflictException("This order has expired.");
            }
            if (request.Amount != order.TotalAmount)
            {
                db.PaymentAttempts.Add(FailedAttempt(order, userId, key, requestHash, request.Amount, now, "AmountMismatch"));
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                throw new ConflictException("Entered amount must equal the order total.");
            }

            var item = order.Items.Single();
            var decremented = await db.Products.Where(product => product.Id == item.ProductId && product.DeletedAt == null
                    && product.IsActive && product.StockQuantity >= item.Quantity)
                .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.StockQuantity, product => product.StockQuantity - item.Quantity), cancellationToken);
            if (decremented != 1)
            {
                db.PaymentAttempts.Add(FailedAttempt(order, userId, key, requestHash, request.Amount, now, "Unavailable"));
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                throw new ConflictException("Product is unavailable or out of stock.");
            }

            order.Status = OrderStatus.Paid;
            order.PaidAt = now;
            db.PaymentAttempts.Add(new PaymentAttempt
            {
                OrderId = order.Id, UserId = userId, IdempotencyKey = key, RequestHash = requestHash,
                EnteredAmount = request.Amount, Status = PaymentStatus.Succeeded, CreatedAt = now, CompletedAt = now
            });
            db.OutboxMessages.Add(new OutboxMessage
            {
                EventType = RabbitMqTopology.OrderPaidEventType,
                OccurredAt = now,
                Payload = JsonSerializer.Serialize(new OrderPaidIntegrationEvent(Guid.NewGuid(), order.Id, userId, order.OrderNumber, order.TotalAmount, order.Currency, now))
            });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Order changed while payment was processing. Please reload it.");
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                throw new ConflictException("Payment was already processed or the idempotency key was reused.");
            }
            return ToResponse(order);
        });
    }

    private async Task EnsureHashAsync(Guid userId, string key, string requestHash, CancellationToken cancellationToken)
    {
        var stored = await db.Orders.AsNoTracking().Where(order => order.UserId == userId && order.IdempotencyKey == key)
            .Select(order => order.RequestHash).SingleAsync(cancellationToken);
        if (!string.Equals(stored, requestHash, StringComparison.Ordinal))
        {
            throw new ConflictException("IdempotencyKey was already used with a different request.");
        }
    }

    private static PaymentAttempt FailedAttempt(Order order, Guid userId, string key, string requestHash, decimal amount,
        DateTimeOffset now, string code) => new()
    {
        OrderId = order.Id, UserId = userId, IdempotencyKey = key, RequestHash = requestHash, EnteredAmount = amount,
        Status = PaymentStatus.Failed, CreatedAt = now, CompletedAt = now, FailureCode = code
    };

    private static OrderResponseDto ToResponse(Order order) => new(order.Id, order.OrderNumber, order.Status.ToString(), order.TotalAmount,
        order.Currency, order.CreatedAt, order.ExpiresAt, order.PaidAt, order.Items.OrderBy(item => item.Id)
            .Select(item => new OrderItemResponseDto(item.ProductId, item.ProductNameSnapshot, item.UnitPrice, item.Quantity)).ToList());
}
