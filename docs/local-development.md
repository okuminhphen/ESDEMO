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

The API loads the root `.env` file for local development without overriding real environment variables. It contains the database connection string, RabbitMQ options, CORS origins and optional Admin bootstrap values. Set `Cors__AllowedOrigins__0=http://localhost:3000`; add indexed values for further frontend URLs. Production deployments must provide the same keys through their environment or secret manager.

Useful endpoints:

```text
GET  http://localhost:5000/health/live
GET  http://localhost:5000/health/ready
GET  http://localhost:5000/api/products
GET  http://localhost:5000/openapi/v1.json
```

## Authentication setup and testing

Set Jwt__Issuer, Jwt__Audience and a random Base64 Jwt__SigningKey in the ignored root .env before starting the API. The signing key is deliberately blank in .env.example. The API exposes register/login/refresh/logout/me and accepts the seeded Admin credentials. See [Authentication](authentication.md) for complete DTO bodies, key generation, refresh/logout behavior and limits.

## Admin product API

After database initialization, log in with the seeded Admin account and pass the returned accessToken in the Authorization: Bearer header. All /api/admin/products endpoints require Admin; a Customer token receives 403. The API provides list/detail/create/update and soft-delete operations. See [Admin products](products.md) for DTOs, PowerShell examples, pagination and the required version on update/delete.

This feature uses the existing Products table and xmin concurrency mapping. An already initialized local database needs no additional migration or seed. The Admin Product frontend, public Customer catalog, mock checkout and private order history are implemented. Start `dotnet run --project backend/src/ESDEMO.Worker` after applying the pending migration to publish events and create notifications; see [RabbitMQ outbox](rabbitmq-outbox.md).

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

The home page calls the API readiness endpoint from the Next.js server. Set `API_BASE_URL` in `frontend/.env.local` to the API address for the active environment. Generate a separate session-encryption key and add it without the `NEXT_PUBLIC_` prefix:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

```dotenv
API_BASE_URL=http://localhost:5000
BFF_SESSION_SECRET=<generated-base64-32-byte-key>
```

The Next.js BFF uses this key to encrypt its `HttpOnly` session cookie. It calls the backend with the access token server-side and refreshes once on a 401; browser JavaScript never receives either token. The frontend currently connects login/register and all Admin Product screens. Read [frontend/README.md](../frontend/README.md) for the route and architecture reference.

## Build and test

```powershell
dotnet build backend/ESDEMO.slnx
dotnet test backend/ESDEMO.slnx
pnpm --dir frontend lint
pnpm --dir frontend build
docker compose config --quiet
```

## Initialize the database and local Admin

The committed `InitialSchema` migration contains Identity and purchasing tables. In the ignored root `.env`, set a strong local password and enable the bootstrap:

```text
SeedAdmin__Enabled=true
SeedAdmin__Email=admin@esdemo.local
SeedAdmin__DisplayName="ESDEMO Administrator"
SeedAdmin__Password="your-strong-local-password"
```

Apply pending migrations and seed the `Admin`/`Customer` roles plus the configured Admin:

```powershell
dotnet run --project backend/src/ESDEMO.Api -- --initialize-database
```

The initializer exits when complete and is safe to run repeatedly. It does not start the API, log the password or run during normal API startup.

For an existing account with the configured email, the initializer preserves its password and ensures it has the Admin role. Changing `SeedAdmin__Password` does not reset an existing account's password. Use only an email you intend to grant administrator access. New bootstrap accounts have their email marked confirmed; this is an explicit operator action, not the public registration flow.

For a future model change, create and review a new migration from the backend directory:

```powershell
cd backend
dotnet tool restore
dotnet ef migrations add DescribeTheChange --project src/ESDEMO.Infrastructure --startup-project src/ESDEMO.Api --output-dir Persistence/Migrations -- --environment Development
```

Review the generated migration and snapshot, then use the initializer to apply it locally. Commit each migration with its model change. PostgreSQL integration tests apply migrations to separate disposable databases; see [test instructions](database-model.md#validation).

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
