# Backend database model

## Current status

The backend defines the persistence model from the purchasing plan and includes the `InitialSchema` migration. The explicit database initializer applies pending migrations, creates the `Admin` and `Customer` roles and optionally creates the configured Admin account. Authentication and business endpoints are not implemented yet.

ApplicationDbContext inherits from IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>. It calls the base Identity mapping first, then loads IEntityTypeConfiguration implementations from Infrastructure.

Identity user/role stores and password/lockout policy are registered in Infrastructure. This enables persistence and bootstrap services; it does not configure JWT, expose login/register or enforce API role policies.

## Ownership and structure

- Domain contains Product, Order, OrderItem, PaymentAttempt, Notification and their status enums. It has no Identity/EF Core dependency.
- Infrastructure owns ApplicationUser, RefreshSession, OutboxMessage and every EF mapping.
- User references in Domain are Guid values. Foreign keys to ApplicationUser are configured only in Infrastructure.
- The existing Identity table names (AspNetUsers, AspNetRoles, AspNetUserRoles and support tables) are retained.
- Business tables use explicit PascalCase names in the public schema.
- All timestamps use DateTimeOffset and PostgreSQL timestamp with time zone. Supply UTC timestamps.
- Product/order/payment/refresh/outbox optimistic concurrency uses an EF shadow uint Version mapped by Npgsql to PostgreSQL xmin. No framework-specific version field is exposed in Domain.

## Tables

| Table | Important fields |
| --- | --- |
| AspNetUsers | Identity fields plus DisplayName, IsActive, CreatedAt; normalized email is required and unique |
| Identity role/support tables | Guid role/user keys, built-in claims, login and token relationships |
| Products | Sku, Name, Description, Price, StockQuantity, IsActive, CreatedAt, UpdatedAt, DeletedAt |
| Orders | UserId, OrderNumber, IdempotencyKey, RequestHash, Status, TotalAmount, Currency, CreatedAt, ExpiresAt, PaidAt |
| OrderItems | OrderId, ProductId, ProductNameSnapshot, UnitPrice, Quantity |
| PaymentAttempts | OrderId, UserId, IdempotencyKey, RequestHash, EnteredAmount, Currency, Provider, Status, CreatedAt, CompletedAt, FailureCode |
| RefreshSessions | UserId, FamilyId, TokenHash, CreatedAt, ExpiresAt, RevokedAt, ReplacedById |
| OutboxMessages | EventType, JSON object Payload, OccurredAt, ProcessedAt, RetryCount, NextAttemptAt, LastError |
| Notifications | UserId, OrderId, SourceEventId, Title, Message, CreatedAt, ReadAt |

No PurchaseHistory table is needed: query paid Orders and their immutable-at-creation item snapshots. BFF/browser sessions remain deferred with frontend work.

## Relationships and deletion

- ApplicationUser has many Orders and RefreshSessions.
- Order has many OrderItems and PaymentAttempts.
- Each OrderItem references a Product.
- PaymentAttempts and Notifications reference Orders through the composite (OrderId, UserId) foreign key. The database rejects an owner that differs from the order owner.
- RefreshSession replacement references (Id, UserId, FamilyId), preventing rotation into another account or token family.
- All added business/auth-session relationships use RESTRICT for deletion. Built-in Identity support relationships keep the Identity defaults.
- Products are discontinued by setting IsActive=false and DeletedAt. Updating only DeletedAt while leaving IsActive=true is rejected.
- No global product query filter is installed: it could unexpectedly hide products from history. Public catalog queries must explicitly filter IsActive and DeletedAt.
- Notifications deliberately have no foreign key to OutboxMessages: outbox records can be archived independently.

## Database invariants

- Unique normalized user email, normalized username, SKU and order number.
- SKU is uppercase ASCII letters/digits with optional underscores/hyphens, maximum 64 characters. Future input handling must normalize it before persistence.
- Product names and snapshots are nonblank and at most 200 characters.
- Prices/amounts are between zero and 999999999999999999 inclusive and must be whole VND.
- Monetary columns use unconstrained numeric plus CHECK constraints. numeric(p, 0) would silently round fractional input before a check could reject it.
- StockQuantity >= 0; Quantity > 0.
- Known string enum values only; currency is VND and payment provider is currently Mock.
- Orders expire after creation. Paid requires a PaidAt between creation and expiry; other states require no PaidAt.
- Payment state determines whether completion time/failure code must be populated.
- Only one Succeeded payment per order via a partial unique index. Failed retries and pending attempts can coexist.
- Order and payment idempotency keys are unique per user within each operation's table. RequestHash is a 64-character hexadecimal digest for future canonical request comparison.
- Refresh tokens store only a 64-character hexadecimal SHA-256 digest of a high-entropy random token. Passwords continue to use Identity PasswordHasher, not SHA-256.
- Refresh expiry/revocation timestamps are checked. A replacement requires revocation, cannot point to itself and cannot have multiple predecessors.
- A SourceEventId can create only one purchase notification for the current consumer.
- Outbox payload is a JSON object; retry count cannot be negative.

Indexes cover user order history, order expiry, payment history, refresh families/expiry, notifications and pending outbox delivery.

## Guarantees still requiring use-case code

These persistence models deliberately do not implement workflow handlers yet. Database constraints do not substitute for domain/application validation.

Future handlers must enforce:

- Ownership and role authorization before queries or writes.
- Server-side price lookup, snapshot creation, nonempty orders and total = sum of item price * quantity.
- Customer-only registration, role assignment, account status, email normalization and password policy.
- Valid state transitions, order expiry, product availability and payment amount = order total.
- Atomic payment + order + inventory + outbox transaction.
- Idempotency-key reuse with a different payload must be rejected, not merely caught as a unique violation.
- Translate concurrency/constraint failures into appropriate application errors and coordinate explicit transactions with EF retry execution strategies.
- Safe refresh rotation, family reuse handling, revocation and token lifetime policy.
- Restrict snapshot/ownership updates; add aggregate behavior as the business commands are implemented.
- Safe multi-worker outbox claiming, delivery confirmation/retry and consumer deduplication.
- Log sanitized error codes rather than credentials or raw broker exception payloads.

The schema does not enforce sums or state consistency across separate rows/tables. In particular, a successful payment row alone does not prove an Order is Paid or inventory was deducted.

## Validation

Regular checks (from the repository root):

```powershell
dotnet build backend/ESDEMO.slnx --configuration Release
dotnet test backend/ESDEMO.slnx --configuration Release --no-build
```

Without ESDEMO_RUN_DATABASE_TESTS=1, the PostgreSQL integration tests are explicitly skipped. The model-generation tests still run without any database connection.

To run integration tests, set these variables in the test process:

- ESDEMO_RUN_DATABASE_TESTS=1
- ESDEMO_TEST_POSTGRES_HOST (default localhost)
- ESDEMO_TEST_POSTGRES_PORT (default 5432)
- ESDEMO_TEST_POSTGRES_USER (default esdemo)
- ESDEMO_TEST_POSTGRES_PASSWORD (required, supply locally; never commit)

Run dotnet test again. Tests connect to the maintenance database postgres, create a database named esdemo_model_tests_<random>, apply the committed migrations, run tests, and drop that exact test database afterward. Use a local/test PostgreSQL account permitted to create databases. The application database esdemo is not used. If the test process is forcibly terminated, a disposable database may remain for manual cleanup.

Coverage includes migration application, idempotent role/Admin seeding, duplicate email, invalid money/stock/status, single successful payment, order ownership foreign keys, history retention, xmin concurrency, refresh family isolation, idempotency uniqueness, JSON outbox validation and event deduplication.

## Initialize a local database

Set the Admin bootstrap values in the ignored root `.env`. The committed example keeps seeding disabled and contains no usable password. Then run from the repository root:

```powershell
dotnet run --project backend/src/ESDEMO.Api -- --initialize-database
```

The command applies pending migrations and performs idempotent Identity bootstrap, then exits without starting the HTTP server. It can be run again safely. Do not use `EnsureCreated` on the application database, and do not run migrations automatically during every API startup.

The next application step is authentication handlers, JWT/refresh-session behavior and API authorization.

## References

- [Identity model customization](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0)
- [Npgsql concurrency tokens](https://www.npgsql.org/efcore/modeling/concurrency.html)
- [EF Core indexes and check constraints](https://learn.microsoft.com/en-us/ef/core/modeling/indexes)
- [PostgreSQL numeric behavior](https://www.postgresql.org/docs/current/datatype-numeric.html)
