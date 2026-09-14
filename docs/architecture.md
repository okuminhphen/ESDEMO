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

The project is intentionally empty until the first domain concept is known. Avoid generic base classes that provide no demonstrated behavior.

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

Contains Entity Framework Core, PostgreSQL configuration, repository implementations and RabbitMQ integration. `ApplicationDbContext` is configured now, while migrations and repositories wait for the first entity.

Do not add a generic `IRepository<TEntity>` by default. Add an aggregate-specific repository when a use case needs persistence behavior that should be expressed in domain terms.

### API

Contains controllers, request/response contracts, middleware, OpenAPI and operational health checks. Controllers translate HTTP input to a use case and HTTP output from a result; they do not own business logic.

The sample `POST /api/examples/validate-text` endpoint demonstrates ASP.NET Core DTO validation. Replace it with the first real feature when the contract pattern is understood.

Validation errors use `ValidationProblemDetails`. Unhandled exceptions use Problem Details and include a trace identifier; exception details are returned only in Development.

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

## RabbitMQ

RabbitMQ is available in local infrastructure, and connection options are validated when the API starts. The baseline does not include a producer, consumer or Worker because no asynchronous use case exists yet.

When messaging is introduced:

- CQRS commands and queries remain in-process application concepts.
- RabbitMQ carries integration events between processes.
- Database changes and outgoing events should use an outbox when they must succeed together.
- Consumers must be idempotent and acknowledge only after successful processing.
- Retry and dead-letter behavior must be explicit.

## Decisions intentionally deferred

- Authentication and authorization mechanism
- Worker process
- Outbox/inbox implementation
- Domain repositories
- Frontend client-state library
- Container images for the API and frontend
- Production deployment topology

These decisions should be made from a real feature or deployment requirement rather than from the starter template.
