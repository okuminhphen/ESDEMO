using System.Text.Json;
using ESDEMO.Domain.Notifications;
using ESDEMO.Infrastructure.Messaging;
using ESDEMO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ESDEMO.Worker.Services;

public sealed class PurchaseNotificationConsumerWorker(
    IServiceScopeFactory scopeFactory,
    ConnectionFactory connectionFactory,
    ILogger<PurchaseNotificationConsumerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(
                    new CreateChannelOptions(publisherConfirmationsEnabled: false, publisherConfirmationTrackingEnabled: false, consumerDispatchConcurrency: 1), stoppingToken);
                await RabbitMqTopology.DeclareAsync(channel, stoppingToken);
                await channel.BasicQosAsync(0, 1, false, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, delivery, stoppingToken);
                await channel.BasicConsumeAsync(RabbitMqTopology.OrderPaidQueue, autoAck: false, consumer, stoppingToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Notification consumer disconnected; reconnecting in five seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<OrderPaidIntegrationEvent>(delivery.Body.Span,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("Order paid event has no body.");
            await SaveNotificationAsync(message, stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Invalid message {MessageId} was sent to the order-paid queue; moving it to DLQ.",
                delivery.BasicProperties.MessageId);
            await channel.BasicNackAsync(delivery.DeliveryTag, false, requeue: false, stoppingToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Message {MessageId} failed during notification processing; moving it to DLQ.",
                delivery.BasicProperties.MessageId);
            await channel.BasicNackAsync(delivery.DeliveryTag, false, requeue: false, stoppingToken);
        }
    }

    private async Task SaveNotificationAsync(OrderPaidIntegrationEvent message, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var exists = await database.Notifications.AsNoTracking()
            .AnyAsync(notification => notification.SourceEventId == message.EventId, cancellationToken);
        if (exists)
        {
            return;
        }

        database.Notifications.Add(new Notification
        {
            UserId = message.UserId,
            OrderId = message.OrderId,
            SourceEventId = message.EventId,
            Title = "Purchase completed",
            Message = $"Order {message.OrderNumber} was paid: {message.TotalAmount:0} {message.Currency}."
        });

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException
            { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            // A worker can crash after SaveChanges but before ACK. The unique event key makes redelivery safe.
        }
    }
}

