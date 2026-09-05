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
ConnectionStrings__PostgreSQL=Host=ucb_db;Port=5432;Database=IMT_Reservas;Username=postgres;Password=<database-password>;Pooling=true;MinPoolSize=2;MaxPoolSize=20
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
dotnet user-secrets set "ConnectionStrings:PostgreSQL" "Host=localhost;Port=5432;Database=IMT_Reservas;Username=postgres;Password=<local-database-password>;Pooling=true;MinPoolSize=2;MaxPoolSize=20"
dotnet user-secrets set "Jwt:Key" "<generated-local-jwt-key>"
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
docker compose --env-file server.env up --build
```

Docker Compose mounts `ucb_dataprotection_keys` at `/app/data-protection-keys`. Back up this volume with the database and do not replace it during routine deployments; it is required to decrypt saved carnet images, profile signatures, and contracts.

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

#### Ownership

Prefer a Google Cloud project owned by the university's Google Workspace or Cloud Identity organization. Grant access to an institution-managed Google Group and keep at least two maintainers; do not leave production OAuth credentials under one student's personal account. A GitHub Organization does not create or own a Google Cloud Organization. It may store source code and deployment workflows, but Google Cloud access is managed separately through IAM.

If the university cannot provide an organization-owned project yet, create the project with a durable institutional Google account, add another institutional maintainer in **IAM & Admin > IAM**, and plan to transfer the project later. The Oracle account does not need to own the Google Cloud project.

#### Google Cloud setup

1. Open [Google Cloud Console](https://console.cloud.google.com/), select the university organization when available, and create separate projects for testing and production.
2. Open **Google Auth Platform > Branding**. Set the application name to `UCB Hold`, choose an institutional support email, and add the public home page, privacy policy and terms URLs from the production domain.
3. Add the production domain under **Authorized domains**. Verify ownership in Google Search Console if Google requests it.
4. Open **Google Auth Platform > Audience**. Choose **Internal** only when the project belongs to the university's Google Workspace or Cloud Identity organization and every user is part of it. Otherwise choose **External**; keep it in testing while developing and add the required institutional accounts as test users.
5. Open **Google Auth Platform > Data Access** and retain only the basic OpenID Connect scopes: `openid`, `email`, and `profile`. UCB Hold does not need Gmail, Drive, Calendar or offline access.
6. Open **Google Auth Platform > Clients**, select **Create client**, choose **Web application**, and name it `UCB Hold Producción`.
7. Under **Authorized redirect URIs**, add the exact public callback `https://YOUR_DOMAIN/api/auth/google/callback`. Do not add a trailing slash. Scheme, host, port, path and letter case must match exactly.
8. For local development, either create a separate web client or add `http://localhost:4200/api/auth/google/callback` only to the testing client. Do not add localhost to the production client.
9. Create the client and copy its client ID and client secret directly into `code/server.env` on the Oracle server. Do not download or commit `client_secret.json`.
10. Publish the app when production is ready. An Internal app normally avoids public brand verification; an External app may require domain and brand verification before general use.

Authorized JavaScript origins are not required by this server-side flow. The callback enters through the public Angular/Nginx address and `/api` forwards it to ASP.NET.

The current `/terminos` route can be used as the terms URL. Before publishing an External client, publish a dedicated, publicly accessible privacy-policy URL on the same domain and have the university review its legal content; do not submit a placeholder URL to Google.

#### Oracle production settings

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

Create `code/server.env` directly on Oracle and restrict it to the deployment account:

```bash
chmod 600 code/server.env
```

Never commit `code/server.env`, `.env`, `client_secret.json`, database passwords, `Jwt__Key`, `Authentication__Google__ClientSecret`, `Email__Password`, private keys or production backups. The repository ignores these files; `code/server.env.example` is intentionally tracked and must contain placeholders only. The Google client ID is not a password, but keeping all environment-specific values together avoids accidental production configuration in source control.

If GitHub Actions performs the deployment, store only the values needed by that workflow in GitHub Actions organization or environment secrets, restrict the production environment, and write `server.env` on Oracle during deployment. Do not upload the complete production environment file as a repository artifact.

When email delivery is disabled, accounts can be created but local verification messages are not sent. Enable and test SMTP before allowing local registration in production.

The frontend runtime image contains only the compiled Angular output served by unprivileged Nginx. Development-only build dependencies are not copied into the production image. Run `npm audit --omit=dev` as the release security gate; also review the full `npm audit` report when updating Angular tooling.

## Empty Database Setup

Create the database:

```bash
psql -U postgres -c "CREATE DATABASE IMT_Reservas;"
```

Apply the data-free schema attached to the release:

```bash
psql -U postgres -d IMT_Reservas -f code/database/schema.sql
```

The release never contains production data or full backups. Keep operational backups in private Oracle storage with restricted access. `schema.sql` initializes an empty database; it is not an upgrade script for a database that already contains data.

## Troubleshooting

| Issue                                  | Resolution                                                                                         |
| -------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `.NET SDK not found`                   | Install .NET 8 SDK and restart the terminal.                                                       |
| PostgreSQL or Redis refuses connection | Run `cd code && docker compose --env-file server.env up -d ucb_db ucb_redis`.                          |
| Backend cannot read secrets            | Run the `dotnet user-secrets` commands from `code/server`.                                         |
| Database schema is outdated            | Review and apply the required `ALTER` statements, or recreate an empty database from `schema.sql`.   |
| Port `4200` is already in use          | Run Angular with another port, for example `ng serve --port 4300`.                                 |
| Frontend dependencies are missing      | Run `npm install` from `code/client`.                                                              |
| Docker backend restarts                | Inspect logs with `docker logs -f ucb_server`.                                                     |
