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
|-- client/      Angular frontend (Feature-Sliced Design under src/)
|-- server/      ASP.NET Core API (modular monolith under src/Modules)
|-- tests/       Backend tests mirroring server modules
|-- database/    Database schema reference
`-- docker-compose.yml
```

Generated files, local reports, IDE metadata and database backups should not be committed.

## Environment Configuration

### Docker

Copy the template and replace every placeholder:

```bash
cp code/server.env.example code/server.env
```

`code/server.env` is ignored by Git. `APP_DB_USER` must match the application role created by `code/database/schema.sql`, and `ConnectionStrings__PostgreSQL` must use that role, never the administrator account.

Load those values into the current shell before running the database commands in this guide:

```bash
set -a; . code/server.env; set +a
```

Generate a development key:

```bash
openssl rand -base64 32
```

Before starting production, provision the Data Protection material through the team's private operational runbook or institutional key-management service. Its generation, rotation and recovery procedures must not be stored in this public repository. The runtime secret directory is ignored by Git and production refuses to start without the required material.

### Local Backend

```bash
cd code/server
dotnet user-secrets set "ConnectionStrings:PostgreSQL" "${ConnectionStrings__PostgreSQL/ucb_db/localhost}"
dotnet user-secrets set "Jwt:Key" "$Jwt__Key"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379"
```

The server project already defines a `UserSecretsId`; do not run `dotnet user-secrets init` again. User Secrets remain outside the repository and are loaded automatically while ASP.NET Core runs in the Development environment. They are intended only for local development, not production storage.

#### Authentication secret reference

Local authentication configuration is stored with ASP.NET Core User Secrets. Production configuration is supplied by the deployment environment or its secret manager. The application consumes these keys:

| Configuration key                        | Secret | Purpose                                                        |
| ---------------------------------------- | ------ | -------------------------------------------------------------- |
| `Authentication:Provider`                | No     | Selects the external identity provider module (`Google`).      |
| `Authentication:InstitutionalDomain`     | No     | Restricts accounts to the institutional email domain.          |
| `Authentication:<Provider>:ClientId`     | No     | Identifies the OAuth web client of the selected provider.      |
| `Authentication:<Provider>:ClientSecret` | Yes    | Authenticates the backend with the selected provider.          |
| `Authentication:FrontendUrl`             | No     | Defines the trusted frontend destination after authentication. |
| `Jwt:Key`                                | Yes    | Signs application access and refresh tokens.                   |
| `ConnectionStrings:PostgreSQL`           | Yes    | Connects the backend to PostgreSQL.                            |
| `Email:Provider`                         | No     | Selects the email delivery module (`Smtp`).                    |
| `Email:From` / `Email:SenderName`        | No     | Sender address and display name used in every message.         |
| `Email:Username`                         | Yes    | Authenticates the configured email sender when required.       |
| `Email:Password`                         | Yes    | Authenticates the configured email sender.                     |

The selected provider requires both `ClientId` and `ClientSecret`; configuring only one prevents the server from starting. Environment-specific values must not appear in tracked configuration, documentation, logs, issues or build artifacts. Do not commit `client_secret.json`, `server.env`, User Secrets output or production credentials.

#### SMTP email for local development

Run the following commands from the repository root. The host, sender and password are read interactively, so no provider or mailbox is written to the shell history. Port `587` uses STARTTLS.

```bash
PROJECT="code/server/UCB_Hold.Server.csproj"

read -rp "Servidor SMTP: " SMTP_HOST
read -rp "Correo remitente: " SMTP_EMAIL
read -rsp "Contraseña del remitente: " SMTP_PASSWORD
echo

dotnet user-secrets set "Email:Enabled" "true" --project "$PROJECT"
dotnet user-secrets set "Email:Provider" "Smtp" --project "$PROJECT"
dotnet user-secrets set "Email:Host" "$SMTP_HOST" --project "$PROJECT"
dotnet user-secrets set "Email:Port" "587" --project "$PROJECT"
dotnet user-secrets set "Email:Username" "$SMTP_EMAIL" --project "$PROJECT"
dotnet user-secrets set "Email:From" "$SMTP_EMAIL" --project "$PROJECT"
dotnet user-secrets set "Email:EnableSsl" "true" --project "$PROJECT"
dotnet user-secrets set "Email:Password" "$SMTP_PASSWORD" --project "$PROJECT"

unset SMTP_HOST SMTP_EMAIL SMTP_PASSWORD
```

#### Branding and organization

Every organization-specific value lives outside the application code:

| Concern                                                                                                               | Location                                                                                                                                     |
| --------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| Product name, organization, campus, unit, inventory code labels, institutional email domain, site URL and asset paths | `code/client/src/shared/config/brand/brand.json`                                                                                             |
| Font stylesheet and origins (also used by the Content Security Policy)                                                | `fonts` in `code/client/src/shared/config/brand/brand.json`                                                                                  |
| Brand colors and font                                                                                                 | `code/client/src/shared/config/brand/brand.css`                                                                                              |
| Logo, favicon, social cover and loan contract template                                                                | `code/client/public/brand/`                                                                                                                  |
| Social cover source                                                                                                   | `code/client/src/shared/config/brand/social-cover.html`                                                                                      |
| Documentation logo                                                                                                    | `docs/brand/logo.png`                                                                                                                        |
| Page title and social metadata                                                                                        | `code/client/src/index.template.html` and `code/client/nginx.template.conf`, rendered by `npm run brand` before `start`, `build` and `watch` |
| Product name and inventory code labels used by the API                                                                | `Branding:*` in `code/server/appsettings.json`                                                                                               |
| Email sender name and domain restriction                                                                              | `Email:SenderName`, `Authentication:InstitutionalDomain`                                                                                     |
| Identity provider name and logo, network/analytics provider and blocked image origins                                 | `code/client/src/shared/config/external-services/external-services.json`                                                                     |
| Browser locale and time zone                                                                                          | `code/client/src/shared/config/region/region.json`                                                                                           |
| API time zone for opening hours                                                                                       | `Region:*` in `code/server/appsettings.json`                                                                                                 |
| Data Protection identity                                                                                              | `DataProtection:ApplicationName`, `PayloadMarker`, `TextMarker`                                                                              |

`DataProtection` identity values encrypt identity documents, signatures and contracts already stored in the database. Keep them unchanged for an existing deployment; change them only for a new organization with an empty database.

#### Replacing an external service

Each external dependency is registered by its own module, selected only through configuration:

| Service           | Module                                            | Contract or settings                                     |
| ----------------- | ------------------------------------------------- | -------------------------------------------------------- |
| Email delivery    | `Shared/Email/EmailServiceRegistration`           | `IEmailSender`, `Email:*`                                |
| External identity | `Modules/Usuarios/Api/AuthenticationRegistration` | `Authentication:Provider`, `Authentication:<Provider>:*` |
| Distributed cache | `Shared/Caching/CacheServiceRegistration`         | `Redis:*`                                                |
| Search            | `Shared/Search/SearchServiceRegistration`         | `ISearchIndex<T>`, `Elasticsearch:*`                     |
| Background jobs   | `Bootstrap/BackgroundJobsRegistration`            | `Hangfire:*`                                             |
| Database          | `Bootstrap/DatabaseServiceRegistration`           | `ConnectionStrings:PostgreSQL`                           |

Changing to another SMTP account only requires new `Email:*` values. A different delivery technology needs a class implementing `IEmailSender`, a new `EmailProvider` value and one case in `EmailServiceRegistration`; services that send messages do not change. A different identity provider needs a new `ExternalIdentityProvider` value and its registration case in `AuthenticationRegistration`; its callback becomes `/api/auth/<provider>/callback`. The institutional domain is configured in `Authentication:InstitutionalDomain` for the API and `institutionalEmailDomain` in `code/client/src/shared/config/brand/brand.json` for the browser.

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

On the first start with an empty volume, PostgreSQL applies `code/database/schema.sql` automatically, including Row Level Security. Enable the application role once the database is healthy:

```bash
docker exec -i ucb_db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v app_user="$APP_DB_USER" -v app_password="$APP_DB_PASSWORD" <<'SQL'
ALTER ROLE :"app_user" WITH LOGIN PASSWORD :'app_password';
SQL
```

Docker Compose mounts the persistent Data Protection key ring and its external protection material with read-only access where applicable. Production validates this configuration at startup. Key lifecycle, backup and recovery are restricted operational responsibilities because the protected carnet images, signatures and contracts depend on them.

| Service     | URL                   |
| ----------- | --------------------- |
| Frontend    | http://localhost:4200 |
| Backend API | http://localhost:5000 |

### Hybrid Local Development

Run backend:

```bash
psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -f code/database/schema.sql
dotnet run --project code/server/UCB_Hold.Server.csproj
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
dotnet build code/UCB_Hold.sln
dotnet test code/tests/UCB_Hold.Tests.csproj
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

### External identity and email

The configured identity provider (Google in this deployment) only provides the OAuth identity. The Angular application, ASP.NET API, PostgreSQL and Redis can continue running on Oracle Cloud or any other host.

The OAuth client type is **Web application** and the application requests only `openid`, `email` and `profile`. This server-side flow does not require an authorized JavaScript origin. Its expected callbacks are:

| Environment | Callback                                           |
| ----------- | -------------------------------------------------- |
| Development | `http://localhost:4200/api/auth/google/callback`   |
| Production  | `https://<public-domain>/api/auth/google/callback` |

#### Production configuration reference

Set these values in `code/server.env` for deployment:

```dotenv
Authentication__Provider=Google
Authentication__FrontendUrl=https://<public-domain>
Authentication__InstitutionalDomain=<institutional-email-domain>
Authentication__Google__ClientId=<google-client-id>
Authentication__Google__ClientSecret=<google-client-secret>
Email__Enabled=true
Email__Provider=Smtp
Email__SenderName=<sender-display-name>
Email__Host=<smtp-host>
Email__Port=587
Email__Username=<smtp-user>
Email__Password=<smtp-password>
Email__From=<sender-address>
Email__EnableSsl=true
```

Replace the bracketed values with the production configuration on Oracle. TLS must terminate at the public reverse proxy, which must preserve `Host`, `X-Forwarded-Proto` and the real client address in `X-Forwarded-For`; behind a CDN, restore that address first (for Cloudflare, `real_ip_header CF-Connecting-IP` with its published ranges in `set_real_ip_from`) or authentication rate limits are shared by every user. The API trusts forwarded headers, so it must listen only on the loopback interface or a private network. The configured frontend URL must use `https`, must not contain a path and should not end in `/`.

Never commit `code/server.env`, `.env`, `client_secret.json`, database passwords, `Jwt__Key`, `Authentication__Google__ClientSecret`, `Email__Password`, private keys or production backups. The repository ignores these files; `code/server.env.example` is intentionally tracked and must contain placeholders only. The Google client ID is not a password, but keeping all environment-specific values together avoids accidental production configuration in source control.

Browser sessions use `HttpOnly`, `SameSite=Strict` cookies. Production marks them `Secure`, so the public site must use HTTPS. Local development through the Angular `/api` proxy works over HTTP because the backend runs in the Development environment; tokens are never written to `sessionStorage` or `localStorage`.

When email delivery is disabled, accounts can be created but local verification messages are not sent. Enable and test SMTP before allowing local registration in production.

The frontend runtime image contains only the compiled Angular output served by unprivileged Nginx. Development-only build dependencies are not copied into the production image. Run `npm audit --omit=dev` as the release security gate; also review the full `npm audit` report when updating Angular tooling.

## Empty Database Setup

Create the database:

```bash
createdb -U "$POSTGRES_USER" "$POSTGRES_DB"
```

Apply the data-free `code/database/schema.sql` from the release source code, which includes the Row Level Security policies, and enable the application role (see [database.md](database.md#row-level-security)):

```bash
psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1 -f code/database/schema.sql
```

```bash
psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v app_user="$APP_DB_USER" -v app_password="$APP_DB_PASSWORD" <<'SQL'
ALTER ROLE :"app_user" WITH LOGIN PASSWORD :'app_password';
SQL
```

The release never contains production data or full backups. Keep operational backups in private Oracle storage with restricted access. `schema.sql` initializes an empty database; it is not an upgrade script for a database that already contains data.

## Troubleshooting

| Issue                                  | Resolution                                                                                         |
| -------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `.NET SDK not found`                   | Install .NET 8 SDK and restart the terminal.                                                       |
| PostgreSQL or Redis refuses connection | Run `cd code && docker compose --env-file server.env up -d ucb_db ucb_redis`.                      |
| Backend cannot read secrets            | Run the `dotnet user-secrets` commands from `code/server`.                                         |
| Database schema is outdated            | Review and apply the required `ALTER` statements, or recreate an empty database from `schema.sql`. |
| Port `4200` is already in use          | Run `npm run brand`, then Angular with another port, for example `npx ng serve --port 4300`.       |
| `Failed to read index HTML file`       | Run `npm run brand`, or use `npm start`/`npm run build` instead of calling `ng` directly.          |
| Frontend dependencies are missing      | Run `npm install` from `code/client`.                                                              |
| Docker backend restarts                | Inspect logs with `docker logs -f ucb_server`.                                                     |
| Data Protection material missing       | Provision the required runtime secrets through the private operational process.                    |
