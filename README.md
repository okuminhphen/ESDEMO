# ESDEMO

ESDEMO is a minimal full-stack starter for learning and building features with clear boundaries. The repository contains a Next.js frontend, an ASP.NET Core API following Clean Architecture, and local PostgreSQL/RabbitMQ infrastructure managed by Docker Compose.

The starter provides health checks, a DTO-validation/MediatR example, tests, and CI. The backend includes the initial Identity and purchasing migration plus an explicit database initializer; business endpoints remain the next step. See [Database model](docs/database-model.md) for the implemented constraints and current limits.

## Technology

- Next.js 16, React 19, TypeScript and Tailwind CSS
- .NET 10 LTS and ASP.NET Core Controller API
- Clean Architecture with CQRS dispatched by MediatR
- Entity Framework Core with PostgreSQL
- RabbitMQ client configuration
- PostgreSQL 18.6 and RabbitMQ 4.3.5 through Docker Compose
- xUnit and GitHub Actions

## Repository structure

```text
ESDEMO/
├── frontend/                    # Next.js App Router application
├── backend/
│   ├── src/
│   │   ├── ESDEMO.Domain/       # Business rules and domain model
│   │   ├── ESDEMO.Application/  # Use cases, CQRS handlers and contracts
│   │   ├── ESDEMO.Infrastructure/# EF Core and external services
│   │   └── ESDEMO.Api/          # Controllers and HTTP contracts
│   ├── tests/ESDEMO.Tests/
│   └── ESDEMO.slnx
├── docs/
├── .github/workflows/ci.yml
└── docker-compose.yml           # PostgreSQL and RabbitMQ only
```

See [Architecture](docs/architecture.md) for layer responsibilities and placement rules.

## Quick start

Requirements:

- .NET SDK 10
- Node.js 24
- pnpm 10
- Docker Desktop with Docker Compose

From the repository root, create the local configuration files and start infrastructure:

```powershell
Copy-Item .env.example .env
Copy-Item frontend/.env.example frontend/.env.local
docker compose up -d
docker compose ps
```

Start the API in a second terminal:

```powershell
dotnet tool restore --tool-manifest backend/.config/dotnet-tools.json
dotnet restore backend/ESDEMO.slnx
dotnet run --project backend/src/ESDEMO.Api -- --initialize-database
dotnet run --project backend/src/ESDEMO.Api
```

Start the frontend in a third terminal:

```powershell
pnpm --dir frontend install
pnpm --dir frontend dev
```

Open these URLs:

| Service | URL |
| --- | --- |
| Frontend | http://localhost:3000 |
| API liveness | http://localhost:5000/health/live |
| API readiness | http://localhost:5000/health/ready |
| OpenAPI document | http://localhost:5000/openapi/v1.json |
| RabbitMQ management | http://localhost:15672 |

Database, RabbitMQ and optional local Admin bootstrap settings are read from `.env`. Set a strong local `SeedAdmin__Password` and enable the seed before running the explicit database initializer. The values in `.env.example` are public examples, not real secrets.

## Verify the repository

```powershell
dotnet build backend/ESDEMO.slnx
dotnet test backend/ESDEMO.slnx
pnpm --dir frontend lint
pnpm --dir frontend build
docker compose config --quiet
```

The readiness endpoint reports PostgreSQL and RabbitMQ separately. The frontend reads this endpoint on the server and displays the current status without requiring the API during frontend build.

## Configuration

Local runtime configuration lives in the ignored root `.env`; copy `.env.example` before running Docker or the API. `appsettings.json` contains only empty configuration shape and safe logging defaults. Production credentials must be supplied by the deployment platform as environment variables or a secret manager and must never be committed.

Common ASP.NET Core overrides:

```text
ConnectionStrings__Database
RabbitMq__Host
RabbitMq__Port
RabbitMq__Username
RabbitMq__Password
RabbitMq__VirtualHost
SeedAdmin__Enabled
SeedAdmin__Email
SeedAdmin__DisplayName
SeedAdmin__Password
```

Frontend server configuration:

```text
API_BASE_URL=http://localhost:5000
```

For complete setup, troubleshooting and migration commands, read [Local development](docs/local-development.md).

## Branch workflow

- `main` stays buildable and represents releasable code.
- `develop` is created from the reviewed initial `main` commit.
- Daily work goes to `develop`; larger changes can use `feature/<name>` branches based on `develop`.
- Releases use a pull request from `develop` to `main`.
- CI must pass before merging into `main`.

See [Contributing](CONTRIBUTING.md) for the suggested Git workflow.

## Adding the first feature

Add one vertical slice at a time. For a future Tasks feature, for example:

```text
ESDEMO.Domain/Tasks/
ESDEMO.Application/Tasks/Commands/CreateTask/
ESDEMO.Application/Tasks/Queries/GetTasks/
ESDEMO.Infrastructure/Persistence/Configurations/TaskConfiguration.cs
ESDEMO.Api/Controllers/TasksController.cs
ESDEMO.Api/Contracts/Tasks/
```

Create repositories, workers, outbox/inbox processing, state-management libraries and other abstractions only when a real use case needs them.

## Stop local infrastructure

```powershell
docker compose down
```

`docker compose down -v` also deletes local PostgreSQL and RabbitMQ data, so use it only when a full local reset is intended.
