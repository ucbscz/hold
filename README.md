<div align="center">

<img src="docs/assets/logo.png" alt="UCB Hold" width="128" />

# UCB Hold

Plataforma full-stack para gestionar reservas, préstamos, mantenimiento y trazabilidad de equipos del Laboratorio de Mecatrónica de la Universidad Católica Boliviana.

<p>
  <a href="#overview">Overview</a>
  ·
  <a href="#architecture">Architecture</a>
  ·
  <a href="#operations">Operations</a>
  ·
  <a href="docs/api.md">API</a>
  ·
  <a href="docs/database.md">Database</a>
  ·
  <a href="CONTRIBUTING.md">Contributing</a>
</p>

[![Tests](https://github.com/alejandroramirezvallejos/UCB_Hold/actions/workflows/tests.yml/badge.svg)](https://github.com/alejandroramirezvallejos/UCB_Hold/actions/workflows/tests.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-21.2-DD0031?style=flat-square&logo=angular)](https://angular.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14+-4169E1?style=flat-square&logo=postgresql)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=flat-square&logo=redis&logoColor=white)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker&logoColor=white)](https://www.docker.com/)

</div>

---

## Overview

UCB Hold centraliza el ciclo operativo de los equipos de laboratorio: consulta de inventario, reservas por disponibilidad, aprobación administrativa, entrega, devolución, contratos, mantenimiento y auditoría. El objetivo del sistema es reducir trabajo manual, evitar conflictos de disponibilidad y conservar trazabilidad clara sobre cada equipo.

### Capabilities

| Domain | Scope |
| --- | --- |
| Inventory | Equipment groups, physical units, accessories, components, categories, furniture and lockers. |
| Reservations | Cart-based request flow, date validation, availability checks and loan state transitions. |
| Administration | CRUD modules, responsive admin tables, audit views and operational notifications. |
| Maintenance | Preventive and corrective maintenance records by provider and equipment detail. |
| Contracts | HTML contract generation associated with approved loans. |
| Security | JWT authentication, refresh tokens, route guards, request validation and private configuration. |

---

## Technology

| Layer | Stack | Responsibility |
| --- | --- | --- |
| Frontend | Angular 21.2, TypeScript, RxJS | Application shell, screens, widgets, forms, guards and HTTP integration. |
| Backend | ASP.NET Core 8, Ardalis.Result, FluentValidation, Mapperly | REST API, business rules, validation and normalized responses. |
| Data | PostgreSQL 14+, EF Core 8, Npgsql | Persistence, native enums, triggers, indexes and projections. |
| Infrastructure | Redis 7, Docker Compose, Nginx | Local services, cache support and containerized delivery. |
| Quality | NUnit, Jasmine/Karma, GitHub Actions, SonarQube | Automated tests, coverage, static analysis and CI verification. |

---

## Architecture

The repository is organized around a small monorepo layout:

```text
.
|-- code/
|   |-- client/      Angular application
|   |-- server/      ASP.NET Core API
|   |-- tests/       Backend automated tests
|   |-- database/    Database bootstrap scripts
|   `-- docker-compose.yml
|-- docs/            Technical documentation and documentation assets
|-- artifacts/       Local/generated artifacts ignored by Git
`-- .github/         CI workflows
```

Frontend modules follow Feature-Sliced Design:

| Slice | Responsibility |
| --- | --- |
| `pages` | Routed screens and page-level composition. |
| `widgets` | Reusable application sections such as navigation, sidebars and panels. |
| `features` | User actions and business flows. |
| `entities` | Domain models, API services and entity-specific UI. |
| `shared` | Cross-cutting utilities, directives, primitives and reusable UI. |

Backend code separates presentation, application rules, core entities and infrastructure. Controllers expose HTTP contracts; services own business decisions; repositories encapsulate persistence.

---

## Quick Start

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
```

Start the stack:

```bash
cd code
docker compose up --build
```

| Service | URL |
| --- | --- |
| Frontend | http://localhost:4200 |
| Backend API | http://localhost:5000 |
| PostgreSQL | localhost:5432 |
| Redis | localhost:6379 |

### Local Development

```bash
cd code
docker compose up -d ucb_db ucb_redis
```

```bash
dotnet tool restore
dotnet ef database update --project code/server/IMT_Reservas.Server.csproj
dotnet run --project code/server/IMT_Reservas.Server.csproj
```

```bash
cd code/client
npm install
npm start
```

Detailed setup instructions are available in [docs/setup.md](docs/setup.md).

---

## Quality Gates

Run the same checks used by CI before opening a pull request:

```bash
dotnet build code/IMT_Reservas.sln
dotnet test code/tests/IMT_Reservas.Tests.csproj
```

```bash
cd code/client
npm run format:check
npx tsc -p tsconfig.app.json --noEmit
npx tsc -p tsconfig.spec.json --noEmit
npm run test:coverage
npm run build
```

Generated coverage and quality outputs must stay out of source control. CI publishes reports as workflow artifacts.

---

## Operations

| Concern | Policy |
| --- | --- |
| Configuration | Local secrets belong in `code/server.env`, environment variables or `dotnet user-secrets`. |
| Database schema | `code/database/schema.sql` documents the database structure maintained by the application. |
| Backups | Database backups are release artifacts, not repository files. Use release assets for `.backup` and `.sql` snapshots. |
| Generated reports | Coverage, SonarQube exports and HTML reports are ignored and uploaded by CI when needed. |
| Releases | Each release should include source changes, migration notes and operational artifacts only when required. |

---

## Documentation

| Document | Purpose |
| --- | --- |
| [docs/setup.md](docs/setup.md) | Local environment, Docker, secrets, verification and troubleshooting. |
| [docs/api.md](docs/api.md) | REST contracts, response format, endpoints and validation rules. |
| [docs/database.md](docs/database.md) | Schema overview, enums, indexes, business rules and restore commands. |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Branching, commits, pull requests and quality expectations. |
| [SECURITY.md](SECURITY.md) | Supported versions, private disclosure and vulnerability handling. |
| [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) | Professional collaboration standards. |

---

## Maintainers

| Maintainer | GitHub |
| --- | --- |
| Josue Balbontin | [josue-balbontin](https://github.com/josue-balbontin) |
| Alejandro Ramirez | [alejandroramirezvallejos](https://github.com/alejandroramirezvallejos) |
| Fernando Terrazas | [FernandoTerrazasLl](https://github.com/FernandoTerrazasLl) |
