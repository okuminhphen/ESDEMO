using RabbitMQ.Client;

namespace ESDEMO.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public const string EventsExchange = "esdemo.events";
    public const string DeadLetterExchange = "esdemo.dead-letter";
    public const string OrderPaidEventType = "OrderPaid.v1";
    public const string OrderPaidRoutingKey = "orders.paid.v1";
    public const string OrderPaidQueue = "esdemo.notifications.order-paid";
    public const string OrderPaidDeadLetterRoutingKey = "orders.paid.dead";
    public const string OrderPaidDeadLetterQueue = "esdemo.notifications.order-paid.dlq";

    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(EventsExchange, ExchangeType.Topic, true, false, null, false, false, cancellationToken);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Direct, true, false, null, false, false, cancellationToken);
        await channel.QueueDeclareAsync(OrderPaidDeadLetterQueue, true, false, false, null, false, false, cancellationToken);
        await channel.QueueBindAsync(OrderPaidDeadLetterQueue, DeadLetterExchange, OrderPaidDeadLetterRoutingKey, null, false, cancellationToken);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-dead-letter-routing-key"] = OrderPaidDeadLetterRoutingKey
        };
        await channel.QueueDeclareAsync(OrderPaidQueue, true, false, false, queueArguments, false, false, cancellationToken);
        await channel.QueueBindAsync(OrderPaidQueue, EventsExchange, OrderPaidRoutingKey, null, false, cancellationToken);
    }
}

