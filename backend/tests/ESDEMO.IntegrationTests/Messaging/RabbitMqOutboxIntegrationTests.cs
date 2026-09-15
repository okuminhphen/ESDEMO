using System.Text.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ESDEMO.Application.Auth.Dtos;
using ESDEMO.Application.Orders.Dtos;
using ESDEMO.Domain.Products;
using ESDEMO.Infrastructure.Messaging;
using ESDEMO.Infrastructure.Persistence;
using ESDEMO.Tests.Integration.Fixtures;
using ESDEMO.Worker.Options;
using ESDEMO.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ESDEMO.Tests.Integration.Messaging;

public sealed class RabbitMqOutboxIntegrationTests(PostgresDatabaseFixture database, RabbitMqFixture rabbitMq)
    : IClassFixture<PostgresDatabaseFixture>, IClassFixture<RabbitMqFixture>
{
    private const string Password = "Valid_Test_Password!2026";

    [Fact]
    public async Task Paid_order_flows_from_API_to_outbox_broker_and_one_notification()
    {
        await using var factory = await CreateApiFactoryAsync();
        using var client = factory.CreateClient();
        var tokens = await RegisterAndLoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var product = await AddProductAsync(factory);
        await using var worker = await WorkerHarness.StartAsync(database.ConnectionString, rabbitMq.CreateConnectionFactory());

        var createdResponse = await client.PostAsJsonAsync("/api/orders", new
        {
            productId = product.Id,
            idempotencyKey = Guid.NewGuid().ToString("N")
        });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var order = (await createdResponse.Content.ReadFromJsonAsync<OrderResponseDto>())!;

        var paidResponse = await client.PostAsJsonAsync($"/api/orders/{order.Id}/pay", new
        {
            amount = product.Price,
            idempotencyKey = Guid.NewGuid().ToString("N")
        });
        Assert.Equal(HttpStatusCode.OK, paidResponse.StatusCode);

        Guid eventId;
        string payload;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var message = (await context.OutboxMessages.AsNoTracking().Where(item => item.EventType == RabbitMqTopology.OrderPaidEventType).ToListAsync()).Single(item => JsonSerializer.Deserialize<OrderPaidIntegrationEvent>(item.Payload)!.OrderId == order.Id);
            eventId = JsonSerializer.Deserialize<OrderPaidIntegrationEvent>(message.Payload)!.EventId;
            payload = message.Payload;
        }

        await EventuallyAsync(async () =>
        {
            await using var context = database.CreateContext();
            var outboxProcessed = await context.OutboxMessages.AnyAsync(item => item.ProcessedAt != null && item.EventType == RabbitMqTopology.OrderPaidEventType);
            var notificationCreated = await context.Notifications.AnyAsync(item => item.SourceEventId == eventId && item.OrderId == order.Id);
            return outboxProcessed && notificationCreated;
        });

        await PublishDuplicateAsync(payload, eventId);
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        await using var assertionContext = database.CreateContext();
        Assert.Equal(1, await assertionContext.Notifications.CountAsync(item => item.SourceEventId == eventId));
    }

    [Fact]
    public async Task Publisher_keeps_outbox_event_and_schedules_retry_when_broker_is_unavailable()
    {
        var eventId = Guid.NewGuid();
        await using (var context = database.CreateContext())
        {
            context.OutboxMessages.Add(new OutboxMessage
            {
                EventType = RabbitMqTopology.OrderPaidEventType,
                Payload = JsonSerializer.Serialize(new OrderPaidIntegrationEvent(eventId, Guid.NewGuid(), Guid.NewGuid(), "RETRY-ORDER", 1, "VND", DateTimeOffset.UtcNow))
            });
            await context.SaveChangesAsync();
        }

        var unavailableBroker = new ConnectionFactory
        {
            HostName = "127.0.0.1",
            Port = 1,
            UserName = "guest",
            Password = "guest",
            RequestedConnectionTimeout = TimeSpan.FromMilliseconds(200)
        };
        await using var worker = await WorkerHarness.StartPublisherOnlyAsync(database.ConnectionString, unavailableBroker);

        await EventuallyAsync(async () =>
        {
            await using var context = database.CreateContext();
            return await context.OutboxMessages.AnyAsync(item => item.EventType == RabbitMqTopology.OrderPaidEventType
                && item.ProcessedAt == null && item.RetryCount > 0 && item.NextAttemptAt != null && item.LeaseId == null);
        });
    }

    private async Task<AuthApiFactory> CreateApiFactoryAsync()
    {
        var factory = new AuthApiFactory(database.ConnectionString, 1000);
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        return factory;
    }

    private static async Task<Product> AddProductAsync(AuthApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var product = new Product
        {
            Sku = $"RABBIT-{Guid.NewGuid():N}".ToUpperInvariant()[..30],
            Name = "Rabbit integration product",
            Price = 25_000,
            StockQuantity = 1,
            IsActive = true
        };
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private static async Task<TokenResponseDto> RegisterAndLoginAsync(HttpClient client)
    {
        var email = $"rabbit-{Guid.NewGuid():N}@example.test";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Rabbit Customer",
            password = Password,
            confirmPassword = Password
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (await login.Content.ReadFromJsonAsync<TokenResponseDto>())!;
    }

    private async Task PublishDuplicateAsync(string payload, Guid eventId)
    {
        await using var connection = await rabbitMq.CreateConnectionFactory().CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(true, true));
        await RabbitMqTopology.DeclareAsync(channel, CancellationToken.None);
        await channel.BasicPublishAsync(RabbitMqTopology.EventsExchange, RabbitMqTopology.OrderPaidRoutingKey, true,
            new BasicProperties { Persistent = true, ContentType = "application/json", MessageId = eventId.ToString("N"), Type = RabbitMqTopology.OrderPaidEventType },
            Encoding.UTF8.GetBytes(payload), CancellationToken.None);
    }

    private static async Task EventuallyAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(20);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
        Assert.Fail("Timed out while waiting for asynchronous processing.");
    }

    private sealed class WorkerHarness : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly OutboxPublisherWorker _publisher;
        private readonly PurchaseNotificationConsumerWorker? _consumer;

        private WorkerHarness(ServiceProvider provider, OutboxPublisherWorker publisher, PurchaseNotificationConsumerWorker? consumer)
        {
            _provider = provider;
            _publisher = publisher;
            _consumer = consumer;
        }

        public static Task<WorkerHarness> StartAsync(string connectionString, ConnectionFactory connectionFactory) =>
            StartCoreAsync(connectionString, connectionFactory, includeConsumer: true);

        public static Task<WorkerHarness> StartPublisherOnlyAsync(string connectionString, ConnectionFactory connectionFactory) =>
            StartCoreAsync(connectionString, connectionFactory, includeConsumer: false);

        private static async Task<WorkerHarness> StartCoreAsync(string connectionString, ConnectionFactory connectionFactory, bool includeConsumer)
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
            services.Configure<OutboxWorkerOptions>(settings =>
            {
                settings.PollingIntervalSeconds = 1;
                settings.LeaseSeconds = 5;
                settings.PublishConfirmationTimeoutSeconds = 2;
                settings.MaxRetryDelaySeconds = 2;
            });
            services.AddSingleton(connectionFactory);
            var provider = services.BuildServiceProvider();
            var publisher = ActivatorUtilities.CreateInstance<OutboxPublisherWorker>(provider);
            var consumer = includeConsumer ? ActivatorUtilities.CreateInstance<PurchaseNotificationConsumerWorker>(provider) : null;
            await publisher.StartAsync(CancellationToken.None);
            if (consumer is not null)
            {
                await consumer.StartAsync(CancellationToken.None);
            }
            return new WorkerHarness(provider, publisher, consumer);
        }

        public async ValueTask DisposeAsync()
        {
            if (_consumer is not null)
            {
                await _consumer.StopAsync(CancellationToken.None);
            }
            await _publisher.StopAsync(CancellationToken.None);
            await _provider.DisposeAsync();
        }
    }
}




