# Local development

## Prerequisites

- .NET SDK 10.0.401 or a compatible newer 10.0 feature band
- Node.js 24
- pnpm 10.33.0
- Docker Desktop

Check the tools:

```powershell
dotnet --version
node --version
pnpm --version
docker --version
docker compose version
```

## Start infrastructure

Docker Compose has safe local defaults, so copying `.env.example` is optional. Copy it when you want to change ports or credentials:

```powershell
Copy-Item .env.example .env
docker compose up -d
docker compose ps
```

Expected published ports:

| Dependency | Port |
| --- | ---: |
| PostgreSQL | 5432 |
| RabbitMQ AMQP | 5672 |
| RabbitMQ management | 15672 |

Both services use named volumes. Restarting containers keeps local data.

Inspect logs when a service is unhealthy:

```powershell
docker compose logs postgres
docker compose logs rabbitmq
```

## Run the backend

From the repository root:

```powershell
dotnet tool restore --tool-manifest backend/.config/dotnet-tools.json
dotnet restore backend/ESDEMO.slnx
dotnet run --project backend/src/ESDEMO.Api
```

Development settings connect to the Docker services at `localhost`. Configuration validation makes the API fail during startup if required production values are missing.

Useful endpoints:

```text
GET  http://localhost:5000/health/live
GET  http://localhost:5000/health/ready
POST http://localhost:5000/api/examples/validate-text
GET  http://localhost:5000/openapi/v1.json
```

## Run the frontend

```powershell
pnpm --dir frontend install
pnpm --dir frontend dev
```

The home page calls the API readiness endpoint from the Next.js server. Override its address in `frontend/.env.local` when required:

```dotenv
API_BASE_URL=http://localhost:5000
```

## Build and test

```powershell
dotnet build backend/ESDEMO.slnx
dotnet test backend/ESDEMO.slnx
pnpm --dir frontend lint
pnpm --dir frontend build
docker compose config --quiet
```

## Add the first EF Core migration

Do this after the first entity and its EF configuration are added:

```powershell
dotnet tool restore --tool-manifest backend/.config/dotnet-tools.json
dotnet ef migrations add InitialCreate --project backend/src/ESDEMO.Infrastructure --startup-project backend/src/ESDEMO.Api --output-dir Persistence/Migrations
dotnet ef database update --project backend/src/ESDEMO.Infrastructure --startup-project backend/src/ESDEMO.Api
```

Commit the migration with the model change.

## Reset local infrastructure

Stop containers while keeping data:

```powershell
docker compose down
```

Remove containers and local database/broker volumes:

```powershell
docker compose down -v
```

The second command permanently removes local Docker data for this Compose project.
