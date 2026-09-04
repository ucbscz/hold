# Setup Guide

This guide describes how to run UCB Hold locally for development, testing and review.

## Requirements

| Tool           | Minimum version      | Check              |
| -------------- | -------------------- | ------------------ |
| .NET SDK       | 8.0 LTS              | `dotnet --version` |
| Node.js        | 22.x LTS             | `node -v`          |
| npm            | Bundled with Node.js | `npm -v`           |
| Docker Desktop | Current stable       | `docker -v`        |
| Git            | Current stable       | `git --version`    |

## Repository Layout

```text
code/
|-- client/      Angular frontend
|-- server/      ASP.NET Core API
|-- tests/       Backend tests
|-- database/    Database schema reference
`-- docker-compose.yml
```

Generated files, local reports, IDE metadata and database backups should not be committed.

## Environment Configuration

### Docker

Create `code/server.env`:

```ini
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
ConnectionStrings__PostgreSQL=Host=ucb_db;Port=5432;Database=IMT_Reservas;Username=postgres;Password=postgres;Pooling=true;MinPoolSize=2;MaxPoolSize=20
Jwt__Key=<local-secret-with-at-least-32-characters>
Redis__ConnectionString=ucb_redis:6379
Redis__Enabled=true
Hangfire__Enabled=true
DataProtection__KeysPath=/app/data-protection-keys
```

Generate a development key:

```bash
openssl rand -base64 32
```

### Local Backend

```bash
cd code/server
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:PostgreSQL" "Host=localhost;Port=5432;Database=IMT_Reservas;Username=postgres;Password=postgres;Pooling=true;MinPoolSize=2;MaxPoolSize=20"
dotnet user-secrets set "Jwt:Key" "local_dev_secret_at_least_32_chars!!"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379"
```

Redis and Hangfire are disabled by default in the Development environment. Enable them when testing the complete local infrastructure:

```bash
dotnet user-secrets set "Redis:Enabled" "true"
dotnet user-secrets set "Hangfire:Enabled" "true"
```

### Frontend

```bash
cd code/client
npm install
```

## Running the Application

### Full Stack with Docker

```bash
cd code
docker compose up --build
```

Docker Compose mounts `ucb_dataprotection_keys` at `/app/data-protection-keys`. Back up this volume with the database and do not replace it during routine deployments; it is required to decrypt saved carnet images, profile signatures, and contracts.

| Service     | URL                   |
| ----------- | --------------------- |
| Frontend    | http://localhost:4200 |
| Backend API | http://localhost:5000 |
| PostgreSQL  | localhost:5432        |
| Redis       | localhost:6379        |

### Hybrid Local Development

Start infrastructure:

```bash
cd code
docker compose up -d ucb_db ucb_redis
```

Run backend:

```bash
psql -U postgres -d IMT_Reservas -f code/database/schema.sql
dotnet run --project code/server/IMT_Reservas.Server.csproj
```

Run frontend:

```bash
cd code/client
npm start
```

| Service     | URL                            |
| ----------- | ------------------------------ |
| Frontend    | http://localhost:4200          |
| Backend API | https://localhost:7216         |
| Swagger     | https://localhost:7216/swagger |

## Verification

Backend:

```bash
dotnet build code/IMT_Reservas.sln
dotnet test code/tests/IMT_Reservas.Tests.csproj
```

Frontend:

```bash
cd code/client
npm run format:check
npx tsc -p tsconfig.app.json --noEmit
npx tsc -p tsconfig.spec.json --noEmit
npm run test:coverage
npm run build
```

### Google and email authentication

Set these values in `code/server.env` for deployment:

```dotenv
Authentication__FrontendUrl=https://your-frontend.example
Authentication__Google__ClientId=your-google-client-id
Authentication__Google__ClientSecret=your-google-client-secret
Email__Enabled=true
Email__Host=smtp.example
Email__Port=587
Email__Username=your-smtp-user
Email__Password=your-smtp-password
Email__From=no-reply@example
Email__EnableSsl=true
```

Register `https://your-frontend.example/api/auth/google/callback` as an authorized redirect URI in Google Cloud because the frontend reverse proxy publishes the API. Local development uses `http://localhost:4200/api/auth/google/callback`. The proxy preserves the original host and protocol so the callback remains correct behind TLS termination. Keep the client secret and SMTP password outside source control. When email delivery is disabled, accounts can be created but local verification messages are not sent.

The frontend runtime image contains only the compiled Angular output served by unprivileged Nginx. Development-only build dependencies are not copied into the production image. Run `npm audit --omit=dev` as the release security gate; also review the full `npm audit` report when updating Angular tooling.

## Database Restore

Create the database:

```bash
psql -U postgres -c "CREATE DATABASE IMT_Reservas;"
```

Restore from the SQL backup attached to a release:

```bash
psql -U postgres -d IMT_Reservas -f artifacts/releases/database/backup.sql
```

Restore from a custom-format backup:

```bash
pg_restore -U postgres -d IMT_Reservas --clean --if-exists artifacts/releases/database/backup.backup
```

## Troubleshooting

| Issue                                  | Resolution                                                                                         |
| -------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `.NET SDK not found`                   | Install .NET 8 SDK and restart the terminal.                                                       |
| PostgreSQL or Redis refuses connection | Run `cd code && docker compose up -d ucb_db ucb_redis`.                                            |
| Backend cannot read secrets            | Run the `dotnet user-secrets` commands from `code/server`.                                         |
| Database schema is outdated            | Restore the current release backup, or recreate an empty database with `code/database/schema.sql`. |
| Port `4200` is already in use          | Run Angular with another port, for example `ng serve --port 4300`.                                 |
| Frontend dependencies are missing      | Run `npm install` from `code/client`.                                                              |
| Docker backend restarts                | Inspect logs with `docker logs -f ucb_server`.                                                     |
