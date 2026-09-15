# Customer catalog and mock checkout

`GET /api/products` is anonymous and returns active, non-deleted products only. Its DTO does not expose stock quantity or internal concurrency values. `GET /api/products/{id}` returns 404 for inactive or deleted products.

Customer order endpoints require the `Customer` policy:

| Method | Path | Purpose |
| --- | --- | --- |
| `POST` | `/api/orders` | Create a single-product pending order |
| `GET` | `/api/orders` | Read the current customer’s order history |
| `GET` | `/api/orders/{id}` | Read one owned order |
| `POST` | `/api/orders/{id}/pay` | Complete a mock payment |

`POST /api/orders` body:

```json
{ "productId": "uuid", "idempotencyKey": "client-generated-key-at-least-16-chars" }
```

`POST /api/orders/{id}/pay` body:

```json
{ "amount": 25000, "idempotencyKey": "another-client-generated-key" }
```

The server snapshots product name and price, uses VND integer values, and expires a pending order after 30 minutes. Payment accepts only the exact server-calculated amount. In one database transaction it conditionally decrements active stock, records a successful payment, marks the order paid, and writes versioned `OrderPaid.v1` to the outbox. A failed amount or unavailable product is recorded as a failed attempt and returns `409`.

Idempotency keys are scoped to a user and operation. Reusing a key with a changed request returns `409`. Order ownership is always obtained from the authenticated user; a supplied user ID is never accepted.

The Next.js BFF exposes fixed `/api/products` and `/api/orders` routes. Browser JavaScript still never receives API tokens. The Worker publishes `OrderPaid.v1` with broker confirms, then creates an idempotent notification. See [RabbitMQ outbox](rabbitmq-outbox.md).
