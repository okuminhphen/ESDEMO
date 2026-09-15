using ESDEMO.Domain.Notifications;
using ESDEMO.Domain.Orders;
using ESDEMO.Domain.Payments;
using ESDEMO.Domain.Products;
using ESDEMO.Infrastructure.Identity;
using ESDEMO.Infrastructure.Messaging;
using ESDEMO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

using ESDEMO.Tests.Integration.Fixtures;
namespace ESDEMO.Tests.Integration.Persistence;

public sealed class PostgresConstraintTests(PostgresDatabaseFixture database)
    : IClassFixture<PostgresDatabaseFixture>
{
    [PostgresFact]
    public async Task Normalized_email_is_unique_in_the_database()
    {
        await using var context = database.CreateContext();
        var user = NewUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var duplicate = NewUser();
        duplicate.NormalizedEmail = user.NormalizedEmail;
        context.Users.Add(duplicate);
        await AssertDatabaseError(context, PostgresErrorCodes.UniqueViolation);
    }

    [PostgresFact]
    public async Task Negative_stock_fractional_money_and_unknown_status_are_rejected()
    {
        var order = await SeedOrder();
        foreach (var statement in new[]
        {
            "UPDATE \"Products\" SET \"StockQuantity\" = -1 WHERE \"Id\" = {0}",
            "UPDATE \"Products\" SET \"Price\" = 100.001 WHERE \"Id\" = {0}",
            "UPDATE \"Products\" SET \"Price\" = 1000000000000000000 WHERE \"Id\" = {0}"
        })
        {
            await using var context = database.CreateContext();
            var error = await Assert.ThrowsAsync<PostgresException>(
                () => context.Database.ExecuteSqlRawAsync(statement, order.Items.Single().ProductId));
            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        }

        await using var statusContext = database.CreateContext();
        var statusError = await Assert.ThrowsAsync<PostgresException>(() => statusContext.Database.ExecuteSqlRawAsync(
            "UPDATE \"Orders\" SET \"Status\" = 'Unknown' WHERE \"Id\" = {0}", order.Id));
        Assert.Equal(PostgresErrorCodes.CheckViolation, statusError.SqlState);
    }

    [PostgresFact]
    public async Task Only_one_successful_payment_per_order_is_allowed()
    {
        var order = await SeedOrder();
        await using (var context = database.CreateContext())
        {
            context.PaymentAttempts.Add(NewPayment(order, PaymentStatus.Succeeded));
            context.PaymentAttempts.Add(NewPayment(order, PaymentStatus.Failed));
            await context.SaveChangesAsync();
        }

        await using var duplicateContext = database.CreateContext();
        duplicateContext.PaymentAttempts.Add(NewPayment(order, PaymentStatus.Succeeded));
        await AssertDatabaseError(duplicateContext, PostgresErrorCodes.UniqueViolation);
    }

    [PostgresFact]
    public async Task Payment_and_notification_cannot_reference_another_users_order()
    {
        var order = await SeedOrder();
        var otherOrder = await SeedOrder();

        await using (var context = database.CreateContext())
        {
            context.PaymentAttempts.Add(NewPayment(order, userId: otherOrder.UserId));
            await AssertDatabaseError(context, PostgresErrorCodes.ForeignKeyViolation);
        }

        await using var notificationContext = database.CreateContext();
        notificationContext.Notifications.Add(NewNotification(order, Guid.NewGuid(), otherOrder.UserId));
        await AssertDatabaseError(notificationContext, PostgresErrorCodes.ForeignKeyViolation);
    }

    [PostgresFact]
    public async Task Product_deletion_is_restricted_and_soft_delete_keeps_order_snapshots()
    {
        var order = await SeedOrder();
        var productId = order.Items.Single().ProductId;

        await using (var context = database.CreateContext())
        {
            var product = await context.Products.SingleAsync(product => product.Id == productId);
            context.Products.Remove(product);
            await AssertDatabaseError(context, PostgresErrorCodes.RestrictViolation);
        }

        await using (var context = database.CreateContext())
        {
            var product = await context.Products.SingleAsync(product => product.Id == productId);
            product.Name = "Renamed";
            product.Price = 500;
            product.IsActive = false;
            product.DeletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        await using var historyContext = database.CreateContext();
        var savedOrder = await historyContext.Orders.Include(order => order.Items).SingleAsync(saved => saved.Id == order.Id);
        Assert.Equal("Original product", savedOrder.Items.Single().ProductNameSnapshot);
        Assert.Equal(100, savedOrder.Items.Single().UnitPrice);
    }

    [PostgresFact]
    public async Task Concurrent_stock_updates_raise_a_concurrency_exception()
    {
        var order = await SeedOrder();
        var productId = order.Items.Single().ProductId;
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var first = await firstContext.Products.SingleAsync(product => product.Id == productId);
        var second = await secondContext.Products.SingleAsync(product => product.Id == productId);

        first.StockQuantity--;
        await firstContext.SaveChangesAsync();
        second.StockQuantity--;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }

    [PostgresFact]
    public async Task Refresh_rotation_cannot_cross_token_families()
    {
        var order = await SeedOrder();
        var now = DateTimeOffset.UtcNow;
        var successor = new RefreshSession
        {
            UserId = order.UserId,
            FamilyId = Guid.NewGuid(),
            TokenHash = NewHash(),
            CreatedAt = now,
            ExpiresAt = now.AddDays(7)
        };
        await using (var context = database.CreateContext())
        {
            context.RefreshSessions.Add(successor);
            await context.SaveChangesAsync();
        }

        await using var invalidContext = database.CreateContext();
        invalidContext.RefreshSessions.Add(new RefreshSession
        {
            UserId = order.UserId,
            FamilyId = Guid.NewGuid(),
            TokenHash = NewHash(),
            CreatedAt = now,
            ExpiresAt = now.AddDays(7),
            RevokedAt = now,
            ReplacedById = successor.Id
        });
        await AssertDatabaseError(invalidContext, PostgresErrorCodes.ForeignKeyViolation);
    }

    [PostgresFact]
    public async Task Same_event_cannot_create_duplicate_notifications()
    {
        var order = await SeedOrder();
        var eventId = Guid.NewGuid();
        await using var context = database.CreateContext();
        context.Notifications.Add(NewNotification(order, eventId));
        await context.SaveChangesAsync();

        context.Notifications.Add(NewNotification(order, eventId));
        await AssertDatabaseError(context, PostgresErrorCodes.UniqueViolation);
    }

    [PostgresFact]
    public async Task Order_and_payment_idempotency_keys_are_unique_per_user()
    {
        var order = await SeedOrder();
        await using (var context = database.CreateContext())
        {
            context.Orders.Add(new Order
            {
                UserId = order.UserId,
                OrderNumber = Guid.NewGuid().ToString("N"),
                IdempotencyKey = order.IdempotencyKey,
                RequestHash = NewHash(),
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
            });
            await AssertDatabaseError(context, PostgresErrorCodes.UniqueViolation);
        }

        await using var paymentContext = database.CreateContext();
        var payment = NewPayment(order);
        paymentContext.PaymentAttempts.Add(payment);
        await paymentContext.SaveChangesAsync();

        paymentContext.PaymentAttempts.Add(NewPayment(order, idempotencyKey: payment.IdempotencyKey));
        await AssertDatabaseError(paymentContext, PostgresErrorCodes.UniqueViolation);
    }

    [PostgresFact]
    public async Task Outbox_requires_JSON_object_and_nonnegative_retry_count()
    {
        foreach (var invalidMessage in new[]
        {
            new OutboxMessage { EventType = "OrderPaid.v1", Payload = "[]", RetryCount = 0 },
            new OutboxMessage { EventType = "OrderPaid.v1", Payload = "{}", RetryCount = -1 }
        })
        {
            await using var context = database.CreateContext();
            context.OutboxMessages.Add(invalidMessage);
            await AssertDatabaseError(context, PostgresErrorCodes.CheckViolation);
        }
    }

    private async Task<Order> SeedOrder()
    {
        await using var context = database.CreateContext();
        var user = NewUser();
        var product = new Product
        {
            Sku = Guid.NewGuid().ToString("N").ToUpperInvariant(),
            Name = "Original product",
            Price = 100,
            StockQuantity = 1
        };
        context.Users.Add(user);
        context.Products.Add(product);
        var order = new Order
        {
            UserId = user.Id,
            OrderNumber = Guid.NewGuid().ToString("N"),
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            RequestHash = NewHash(),
            TotalAmount = product.Price,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
        };
        order.Items.Add(new OrderItem
        {
            OrderId = order.Id,
            ProductId = product.Id,
            ProductNameSnapshot = product.Name,
            UnitPrice = product.Price,
            Quantity = 1
        });
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order;
    }

    private static ApplicationUser NewUser()
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        return new ApplicationUser
        {
            DisplayName = "Test user",
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant()
        };
    }

    private static PaymentAttempt NewPayment(
        Order order,
        PaymentStatus status = PaymentStatus.Pending,
        Guid? userId = null,
        string? idempotencyKey = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new PaymentAttempt
        {
            OrderId = order.Id,
            UserId = userId ?? order.UserId,
            IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString("N"),
            RequestHash = NewHash(),
            EnteredAmount = order.TotalAmount,
            Status = status,
            CreatedAt = now,
            CompletedAt = status == PaymentStatus.Pending ? null : now,
            FailureCode = status == PaymentStatus.Failed ? "MockDeclined" : null
        };
    }

    private static Notification NewNotification(Order order, Guid eventId, Guid? userId = null) =>
        new()
        {
            UserId = userId ?? order.UserId,
            OrderId = order.Id,
            SourceEventId = eventId,
            Title = "Purchase completed",
            Message = "Test notification"
        };

    private static string NewHash() => $"{Guid.NewGuid():N}{Guid.NewGuid():N}";

    private static async Task AssertDatabaseError(ApplicationDbContext context, string sqlState)
    {
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Equal(sqlState, Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }
}




