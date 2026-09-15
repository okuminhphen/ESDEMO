using System.Text;
using ESDEMO.Infrastructure.Messaging;
using ESDEMO.Infrastructure.Persistence;
using ESDEMO.Worker.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ESDEMO.Worker.Services;

public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    ConnectionFactory connectionFactory,
    IOptions<OutboxWorkerOptions> options,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var leaseId = Guid.NewGuid();
                var now = DateTimeOffset.UtcNow;
                var messages = await ClaimDueMessagesAsync(leaseId, now, settings, stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        await PublishAsync(message, settings, stoppingToken);
                        await MarkProcessedAsync(message.Id, leaseId, DateTimeOffset.UtcNow, stoppingToken);
                        logger.LogInformation("Outbox event {EventId} ({EventType}) was published.", message.Id, message.EventType);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        await ScheduleRetryAsync(message, leaseId, settings, exception, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox publisher loop failed; pending messages will be retried.");
            }

            await Task.Delay(TimeSpan.FromSeconds(settings.PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task<IReadOnlyList<OutboxMessage>> ClaimDueMessagesAsync(
        Guid leaseId, DateTimeOffset now, OutboxWorkerOptions settings, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var candidateIds = await database.OutboxMessages.AsNoTracking()
            .Where(message => message.ProcessedAt == null
                && (message.NextAttemptAt == null || message.NextAttemptAt <= now)
                && (message.LeaseExpiresAt == null || message.LeaseExpiresAt <= now))
            .OrderBy(message => message.OccurredAt)
            .Select(message => message.Id)
            .Take(settings.BatchSize)
            .ToListAsync(cancellationToken);

        if (candidateIds.Count == 0)
        {
            return [];
        }

        var leaseExpiresAt = now.AddSeconds(settings.LeaseSeconds);
        await database.OutboxMessages
            .Where(message => candidateIds.Contains(message.Id)
                && message.ProcessedAt == null
                && (message.NextAttemptAt == null || message.NextAttemptAt <= now)
                && (message.LeaseExpiresAt == null || message.LeaseExpiresAt <= now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.LeaseId, leaseId)
                .SetProperty(message => message.LeaseExpiresAt, leaseExpiresAt), cancellationToken);

        return await database.OutboxMessages.AsNoTracking()
            .Where(message => message.LeaseId == leaseId)
            .OrderBy(message => message.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    private async Task PublishAsync(OutboxMessage message, OutboxWorkerOptions settings, CancellationToken cancellationToken)
    {
        using var confirmationTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        confirmationTimeout.CancelAfter(TimeSpan.FromSeconds(settings.PublishConfirmationTimeoutSeconds));

        await using var connection = await connectionFactory.CreateConnectionAsync(confirmationTimeout.Token);
        await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true), confirmationTimeout.Token);
        await RabbitMqTopology.DeclareAsync(channel, confirmationTimeout.Token);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = message.EventType,
            MessageId = message.Id.ToString("N"),
            Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds())
        };
        await channel.BasicPublishAsync(
            RabbitMqTopology.EventsExchange,
            RabbitMqTopology.OrderPaidRoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(message.Payload),
            cancellationToken: confirmationTimeout.Token);
    }

    private async Task MarkProcessedAsync(Guid messageId, Guid leaseId, DateTimeOffset processedAt, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await database.OutboxMessages
            .Where(message => message.Id == messageId && message.LeaseId == leaseId && message.ProcessedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.ProcessedAt, processedAt)
                .SetProperty(message => message.LeaseId, (Guid?)null)
                .SetProperty(message => message.LeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(message => message.LastError, (string?)null), cancellationToken);
    }

    private async Task ScheduleRetryAsync(OutboxMessage message, Guid leaseId, OutboxWorkerOptions settings,
        Exception exception, CancellationToken cancellationToken)
    {
        var retryCount = message.RetryCount + 1;
        var exponent = Math.Min(retryCount - 1, 8);
        var delay = Math.Min(settings.MaxRetryDelaySeconds, 1 << exponent);
        var nextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(delay);
        var error = $"{exception.GetType().Name}: {exception.Message}";
        if (error.Length > 2000)
        {
            error = error[..2000];
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await database.OutboxMessages
            .Where(item => item.Id == message.Id && item.LeaseId == leaseId && item.ProcessedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.RetryCount, retryCount)
                .SetProperty(item => item.NextAttemptAt, nextAttemptAt)
                .SetProperty(item => item.LastError, error)
                .SetProperty(item => item.LeaseId, (Guid?)null)
                .SetProperty(item => item.LeaseExpiresAt, (DateTimeOffset?)null), cancellationToken);

        logger.LogWarning(exception,
            "Outbox event {EventId} failed to publish (attempt {RetryCount}); retry scheduled for {NextAttemptAt}. Updated={Updated}.",
            message.Id, retryCount, nextAttemptAt, updated);
    }
}
