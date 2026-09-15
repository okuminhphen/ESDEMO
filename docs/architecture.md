# Architecture

## Goals

This baseline keeps architecture visible without making an intern or fresher navigate unnecessary abstractions. Project references enforce the main boundary, while business folders are added only when a real feature exists.

## Backend dependency rule

```text
ESDEMO.Domain
      ↑
ESDEMO.Application
      ↑
ESDEMO.Infrastructure
      ↑
ESDEMO.Api
```

The physical references are:

- `Application` references `Domain`.
- `Infrastructure` references `Application` and `Domain`.
- `Api` references `Application` and `Infrastructure`.
- `Domain` references no other project.

`Api` acts as the composition root. Its reference to `Infrastructure` is used to register concrete implementations with dependency injection.

## Layer responsibilities

### Domain

Contains entities, value objects, aggregates, domain services, domain events and business rules. It must not reference Entity Framework Core, RabbitMQ or ASP.NET Core.

The Domain now defines Product, Order, OrderItem, PaymentAttempt and Notification. Identity-specific user/session types and OutboxMessage live in Infrastructure. Workflow behavior will be added with each use case; see [Database model](database-model.md) for current guarantees and deferred rules.

### Application

Contains use cases, CQRS commands/queries, handlers, validation rules and interfaces required from external systems. Commands and queries implement MediatR request contracts, are dispatched through `ISender`, and handlers are registered from the Application assembly.

A future feature should be grouped by use case:

```text
Tasks/
├── Commands/
│   └── CreateTask/
│       ├── CreateTaskCommand.cs
│       ├── CreateTaskCommandHandler.cs
│       └── CreateTaskValidator.cs
└── Queries/
    └── GetTasks/
        ├── GetTasksQuery.cs
        ├── GetTasksQueryHandler.cs
        └── TaskListItem.cs
```

Commands change state. Queries only read and return application models. Both may initially use the same PostgreSQL database.

### Infrastructure

Contains Entity Framework Core, Identity stores, PostgreSQL configuration and RabbitMQ integration. `ApplicationDbContext` derives from IdentityDbContext and loads entity configurations from this assembly. The model and initial migration are implemented. The explicit database initializer applies pending migrations and seeds roles plus an optional configured Admin. Product persistence uses a feature-specific EF repository; messaging workers remain pending.

Do not add a generic `IRepository<TEntity>` by default. Add an aggregate-specific repository when a use case needs persistence behavior that should be expressed in domain terms.

### API

Contains controllers, request/response contracts, middleware, OpenAPI and operational health checks. Controllers translate HTTP input to a use case and HTTP output from a result; they do not own business logic.

The sample `POST /api/examples/validate-text` endpoint demonstrates ASP.NET Core DTO validation. Replace it with the first real feature when the contract pattern is understood.

Validation errors use `ValidationProblemDetails`. Other errors use Problem Details and include a trace identifier. Auth failures never expose provider details, including in Development.

Auth DTOs and DataAnnotations live in Application/Auth/Dtos and are shared with the API. MediatR validates each auth command payload before dispatch. Infrastructure implements IAuthService using Identity and transactional PostgreSQL operations; controllers do not expose Identity entities. JWT middleware checks account status and current roles. See [Authentication](authentication.md).

Product DTOs live in Application/Products/Dtos and receive validation at both the HTTP boundary and MediatR dispatch. Application handlers own create/update/soft-delete behavior and depend on a Products repository interface in Application. Infrastructure implements that interface with EF Core, including DTO projection, search, paging and persistence-error translation. Every /api/admin/products endpoint requires the Admin policy. Responses expose an explicit version value from PostgreSQL xmin; update/delete require the last-read version to reject stale writes with 409. See [Admin products](products.md).

## Frontend structure

`frontend/src/app` owns routes, layouts, loading/error boundaries and page composition. Reusable code belongs outside the route tree:

```text
src/
├── app/
├── components/
├── features/
├── lib/
└── types/
```

Use Server Components by default. Add a Client Component when browser APIs, local interaction or client-side state are required. Server Components should call the .NET API directly through `lib/api`; a BFF Route Handler should be added only for a concrete need such as cookie-based session handling.

## PostgreSQL and EF Core

- PostgreSQL is the system of record.
- EF Core configuration and migrations belong to Infrastructure.
- Entity configuration uses `IEntityTypeConfiguration<TEntity>` rather than large configuration blocks in `DbContext`.
- Read-only queries should project to DTOs and use no tracking.
- A migration is committed together with the model change that requires it.
- Admin product CRUD uses the existing Products schema and xmin mapping; it does not require a new migration. Each product mutation is saved atomically with SaveChanges; this feature needs neither a transaction spanning multiple saves nor RabbitMQ events.

## RabbitMQ

RabbitMQ is available in local infrastructure, and connection options are validated when the API starts. The baseline does not include a producer, consumer or Worker because no asynchronous use case exists yet.

When messaging is introduced:

- CQRS commands and queries remain in-process application concepts.
- RabbitMQ carries integration events between processes.
- Database changes and outgoing events should use an outbox when they must succeed together.
- Consumers must be idempotent and acknowledge only after successful processing.
- Retry and dead-letter behavior must be explicit.

## Decisions intentionally deferred

- Frontend/BFF sessions, email verification, password recovery and Admin MFA
- Worker process
- Outbox/inbox implementation
- Repositories for ordering and payment use cases
- Frontend client-state library
- Container images for the API and frontend
- Production deployment topology

These decisions should be made from a real feature or deployment requirement rather than from the starter template.
