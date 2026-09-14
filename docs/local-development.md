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

Docker Compose and the API require the ignored root `.env` file. Create it from the committed example before starting infrastructure:

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

The API loads the root `.env` file for local development without overriding real environment variables. It contains the database connection string, RabbitMQ options, and CORS origins. Set `Cors__AllowedOrigins__0=http://localhost:3000`; add indexed values for further frontend URLs. Production deployments must provide the same keys through their environment or secret manager.

Useful endpoints:

```text
GET  http://localhost:5000/health/live
GET  http://localhost:5000/health/ready
POST http://localhost:5000/api/examples/validate-text
GET  http://localhost:5000/openapi/v1.json
```

## Request logs

The API writes structured request logs to the terminal running `dotnet run`. Each entry includes the HTTP method, path, status code, elapsed time and trace ID. Request bodies, authorization headers, cookies and query strings are intentionally excluded.

Successful requests are logged at `Information`, client errors at `Warning`, and server errors at `Error`. Health endpoints use `Debug` to keep normal development output quiet.

## Run the frontend

Create the ignored frontend environment file once:

```powershell
Copy-Item frontend/.env.example frontend/.env.local
```

Then run:

```powershell
pnpm --dir frontend install
pnpm --dir frontend dev
```

The home page calls the API readiness endpoint from the Next.js server. Set `API_BASE_URL` in `frontend/.env.local` to the API address for the active environment.

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
