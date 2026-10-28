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
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<database-password>
POSTGRES_DB=IMT_Reservas
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__PostgreSQL=Host=ucb_db;Port=5432;Database=IMT_Reservas;Username=ucbhold_app;Password=<app-role-password>;Pooling=true;MinPoolSize=2;MaxPoolSize=20
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

Before starting production, provision the Data Protection material through the team's private operational runbook or institutional key-management service. Its generation, rotation and recovery procedures must not be stored in this public repository. The runtime secret directory is ignored by Git and production refuses to start without the required material.

### Local Backend

```bash
cd code/server
dotnet user-secrets set "ConnectionStrings:PostgreSQL" "Host=localhost;Port=5432;Database=IMT_Reservas;Username=postgres;Password=<local-database-password>;Pooling=true;MinPoolSize=2;MaxPoolSize=20"
dotnet user-secrets set "Jwt:Key" "<generated-local-jwt-key>"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379"
```

The server project already defines a `UserSecretsId`; do not run `dotnet user-secrets init` again. User Secrets remain outside the repository and are loaded automatically while ASP.NET Core runs in the Development environment. They are intended only for local development, not production storage.

#### Authentication secret reference

Local authentication configuration is stored with ASP.NET Core User Secrets. Production configuration is supplied by the deployment environment or its secret manager. The application consumes these keys:

| Configuration key                    | Secret | Purpose                                                        |
| ------------------------------------ | ------ | -------------------------------------------------------------- |
| `Authentication:Google:ClientId`     | No     | Identifies the Google OAuth web client.                        |
| `Authentication:Google:ClientSecret` | Yes    | Authenticates the backend with the Google OAuth client.        |
| `Authentication:FrontendUrl`         | No     | Defines the trusted frontend destination after authentication. |
| `Jwt:Key`                            | Yes    | Signs application access and refresh tokens.                   |
| `ConnectionStrings:PostgreSQL`       | Yes    | Connects the backend to PostgreSQL.                            |
| `Email:Username`                     | Yes    | Authenticates the configured email sender when required.       |
| `Email:Password`                     | Yes    | Authenticates the configured email sender.                     |

Google OAuth requires both `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret`; configuring only one prevents the server from starting. Environment-specific values must not appear in tracked configuration, documentation, logs, issues or build artifacts. Do not commit `client_secret.json`, `server.env`, User Secrets output or production credentials.

#### Titan email for local development

Run the following commands from the repository root. The password is entered without being displayed or stored in the shell history. Port `587` uses STARTTLS and is compatible with the backend email client.

```bash
PROJECT="code/server/IMT_Reservas.Server.csproj"

read -rp "Correo remitente de Titan: " SMTP_EMAIL

dotnet user-secrets set "Email:Enabled" "true" --project "$PROJECT"
dotnet user-secrets set "Email:Host" "smtp.titan.email" --project "$PROJECT"
dotnet user-secrets set "Email:Port" "587" --project "$PROJECT"
dotnet user-secrets set "Email:Username" "$SMTP_EMAIL" --project "$PROJECT"
dotnet user-secrets set "Email:From" "$SMTP_EMAIL" --project "$PROJECT"
dotnet user-secrets set "Email:EnableSsl" "true" --project "$PROJECT"

read -rsp "Contraseña de la cuenta Titan: " SMTP_PASSWORD
echo
dotnet user-secrets set "Email:Password" "$SMTP_PASSWORD" --project "$PROJECT"

unset SMTP_EMAIL SMTP_PASSWORD
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
docker compose --env-file server.env up --build
```

Docker Compose mounts the persistent Data Protection key ring and its external protection material with read-only access where applicable. Production validates this configuration at startup. Key lifecycle, backup and recovery are restricted operational responsibilities because the protected carnet images, signatures and contracts depend on them.

| Service     | URL                   |
| ----------- | --------------------- |
| Frontend    | http://localhost:4200 |
| Backend API | http://localhost:5000 |

### Hybrid Local Development

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

Google Cloud only provides the OAuth identity in this deployment. The Angular application, ASP.NET API, PostgreSQL and Redis can continue running on Oracle Cloud or any other host.

The OAuth client type is **Web application** and the application requests only `openid`, `email` and `profile`. This server-side flow does not require an authorized JavaScript origin. Its expected callbacks are:

| Environment | Callback                                           |
| ----------- | -------------------------------------------------- |
| Development | `http://localhost:4200/api/auth/google/callback`   |
| Production  | `https://<public-domain>/api/auth/google/callback` |

#### Production configuration reference

Set these values in `code/server.env` for deployment:

```dotenv
Authentication__FrontendUrl=https://<public-domain>
Authentication__Google__ClientId=<google-client-id>
Authentication__Google__ClientSecret=<google-client-secret>
Email__Enabled=true
Email__Host=<smtp-host>
Email__Port=587
Email__Username=<smtp-user>
Email__Password=<smtp-password>
Email__From=<sender-address>
Email__EnableSsl=true
```

Replace the bracketed values with the production configuration on Oracle. TLS must terminate at the public reverse proxy, which must preserve `Host` and `X-Forwarded-Proto`. The configured frontend URL must use `https`, must not contain a path and should not end in `/`.

Never commit `code/server.env`, `.env`, `client_secret.json`, database passwords, `Jwt__Key`, `Authentication__Google__ClientSecret`, `Email__Password`, private keys or production backups. The repository ignores these files; `code/server.env.example` is intentionally tracked and must contain placeholders only. The Google client ID is not a password, but keeping all environment-specific values together avoids accidental production configuration in source control.

Browser sessions use `HttpOnly`, `SameSite=Strict` cookies. Production marks them `Secure`, so the public site must use HTTPS. Local development through the Angular `/api` proxy works over HTTP because the backend runs in the Development environment; tokens are never written to `sessionStorage` or `localStorage`.

When email delivery is disabled, accounts can be created but local verification messages are not sent. Enable and test SMTP before allowing local registration in production.

The frontend runtime image contains only the compiled Angular output served by unprivileged Nginx. Development-only build dependencies are not copied into the production image. Run `npm audit --omit=dev` as the release security gate; also review the full `npm audit` report when updating Angular tooling.

## Empty Database Setup

Create the database:

```bash
psql -U postgres -c "CREATE DATABASE IMT_Reservas;"
```

Apply the data-free schema attached to the release, which includes the Row Level Security policies, and enable the application role (see [database.md](database.md#row-level-security)):

```bash
psql -U postgres -d IMT_Reservas -f code/database/schema.sql
psql -U postgres -d IMT_Reservas -c "ALTER ROLE ucbhold_app WITH LOGIN PASSWORD '<secret>';"
```

The release never contains production data or full backups. Keep operational backups in private Oracle storage with restricted access. `schema.sql` initializes an empty database; it is not an upgrade script for a database that already contains data.

## Troubleshooting

| Issue                                  | Resolution                                                                                         |
| -------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `.NET SDK not found`                   | Install .NET 8 SDK and restart the terminal.                                                       |
| PostgreSQL or Redis refuses connection | Run `cd code && docker compose --env-file server.env up -d ucb_db ucb_redis`.                      |
| Backend cannot read secrets            | Run the `dotnet user-secrets` commands from `code/server`.                                         |
| Database schema is outdated            | Review and apply the required `ALTER` statements, or recreate an empty database from `schema.sql`. |
| Port `4200` is already in use          | Run Angular with another port, for example `ng serve --port 4300`.                                 |
| Frontend dependencies are missing      | Run `npm install` from `code/client`.                                                              |
| Docker backend restarts                | Inspect logs with `docker logs -f ucb_server`.                                                     |
| Data Protection material missing       | Provision the required runtime secrets through the private operational process.                    |
