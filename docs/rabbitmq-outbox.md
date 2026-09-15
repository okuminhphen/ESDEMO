# RabbitMQ outbox worker

The API does not publish to RabbitMQ inside the payment HTTP request. Completing a payment performs one PostgreSQL transaction: it decrements stock conditionally, sets the order to `Paid`, inserts the successful payment attempt, and appends one `OrderPaid.v1` JSON event to `OutboxMessages`. The request can therefore succeed even when RabbitMQ is temporarily unavailable, without losing the business event.

Run the database initializer after pulling this change, then start the Worker in a separate terminal:

```powershell
dotnet run --project backend/src/ESDEMO.Api -- --initialize-database
dotnet run --project backend/src/ESDEMO.Worker
```

The Worker loads the same ignored root `.env` for local execution; deployment environment variables override it. It requires the existing `ConnectionStrings__Database` and `RabbitMq__*` values. It deliberately does not load the JWT signing key. It never exposes an HTTP port.

## Broker topology

| Resource | Name | Purpose |
| --- | --- | --- |
| topic exchange | `esdemo.events` | Durable integration event exchange |
| routing key | `orders.paid.v1` | Route `OrderPaid.v1` events |
| durable queue | `esdemo.notifications.order-paid` | Notification consumer input |
| direct exchange | `esdemo.dead-letter` | Dead-letter exchange |
| durable queue | `esdemo.notifications.order-paid.dlq` | Messages that cannot be parsed or processed |

The Worker declares this topology on startup. A broker restart preserves it because the exchanges and queues are durable and the publisher marks messages persistent.

## Delivery and failure behaviour

1. The publisher selects due unprocessed Outbox rows and claims them with a database lease. Another Worker cannot claim the row until the lease expires.
2. It publishes with RabbitMQ publisher confirms enabled. A row receives `ProcessedAt` only after the broker confirms the publish.
3. Connection, broker or confirm failures keep the row in PostgreSQL, record a sanitized `LastError`, and retry with exponential backoff capped by `OutboxWorker__MaxRetryDelaySeconds`. Events are never discarded just because RabbitMQ is down.
4. RabbitMQ delivery is at least once: a Worker crash after broker confirmation but before `ProcessedAt` can publish the same event again.
5. The consumer deduplicates on the unique `Notifications.SourceEventId` index. Redelivery therefore cannot make duplicate customer notifications.
6. Invalid JSON or a processing failure is rejected with `requeue=false`; RabbitMQ puts it in the DLQ. Inspect and fix a DLQ message before replaying it deliberately.

`OutboxMessages.LeaseId` and `LeaseExpiresAt` are operational fields introduced by the `AddOutboxLeases` migration. They support several publisher replicas while retaining a short recovery window if one process dies.

## Runtime tuning

These optional root `.env` values have safe development defaults:

```text
OutboxWorker__BatchSize=20
OutboxWorker__PollingIntervalSeconds=2
OutboxWorker__LeaseSeconds=30
OutboxWorker__MaxRetryDelaySeconds=300
OutboxWorker__PublishConfirmationTimeoutSeconds=10
```

Keep the lease longer than the expected broker confirm timeout. In production supply RabbitMQ/PostgreSQL credentials and these settings through platform environment variables or a secret manager; do not commit `.env`.

## Verification

1. Start Docker, initialize the database, then run API and Worker.
2. Complete a mock payment through the customer flow.
3. In RabbitMQ Management, the notification queue should drain to zero; its DLQ should remain empty.
4. Query `OutboxMessages`: the row has `ProcessedAt`; query `Notifications`: one row exists for the order/user.

The integration suite runs this flow against disposable PostgreSQL and RabbitMQ Testcontainers: payment API → Outbox → publisher confirm → consumer → one notification, then deliberately republishes the same event to prove deduplication. A second test uses an unreachable broker and verifies that the event remains pending with retry/backoff. You can still perform the same outage exercise manually by stopping RabbitMQ, paying an order, then starting it again.
