# Database Guide

UCB Hold uses PostgreSQL 16+ with Entity Framework Core 8. The database name, administrator account and application role come from `code/server.env`. Repository source and releases contain only the data-free schema reference in [`code/database/schema.sql`](../code/database/schema.sql). Production backups remain in private infrastructure.

## Entity Relationship Diagram

![Entity relationship diagram](assets/diagram.png)

## Schema Overview

| Table                     | Purpose                                                                                                      | Soft delete |
| ------------------------- | ------------------------------------------------------------------------------------------------------------ | ----------- |
| `usuarios`                | User identity, contact data and role.                                                                        | Yes         |
| `codigos_autenticacion`   | Hashed, expiring, single-use Google exchange codes.                                                          | No          |
| `prestamos`               | Loan lifecycle, user ownership and date range.                                                               | Yes         |
| `detalles_prestamos`      | Equipment groups requested in each loan.                                                                     | Yes         |
| `grupos_equipos`          | Catalog-level grouping, including the maximum loan duration shared by equivalent units.                      | Yes         |
| `equipos`                 | Physical equipment units with unique `codigo_imt` and `codigo_ucb` identifiers, serial number and condition. | Yes         |
| `ambientes`               | Named rooms referenced by `equipos.id_ambiente`.                                                             | Yes         |
| `procedencias`            | Acquisition origins referenced by `equipos.id_procedencia`.                                                  | Yes         |
| `configuraciones_sistema` | Global opening hours and weekly/date exceptions in `horarios` JSONB.                                         | No          |
| `categorias`              | Equipment classification.                                                                                    | Yes         |
| `carreras`                | Academic programs associated with users.                                                                     | Yes         |
| `muebles`                 | Storage furniture.                                                                                           | Yes         |
| `gaveteros`               | Storage lockers inside furniture.                                                                            | Yes         |
| `accesorios`              | Accessories assigned to equipment groups.                                                                    | Yes         |
| `componentes`             | Internal or related components.                                                                              | Yes         |
| `empresas_mantenimiento`  | Maintenance providers.                                                                                       | Yes         |
| `mantenimientos`          | Maintenance events.                                                                                          | Yes         |
| `detalles_mantenimientos` | Equipment involved in a maintenance event.                                                                   | Yes         |
| `contratos`               | Generated contract HTML for loans.                                                                           | No          |

## PostgreSQL Enums

| Enum                 | Values                                                                                  | Used by                   |
| -------------------- | --------------------------------------------------------------------------------------- | ------------------------- |
| `estado_prestamo`    | `pendiente`, `aprobado`, `activo`, `atrasado`, `finalizado`, `rechazado`, `cancelado`     | `prestamos`               |
| `estado_equipo`      | `operativo`, `parcialmente_operativo`, `inoperativo`                                    | `equipos`                 |
| `tipo_usuario`       | `docente`, `administrativo`, `administrador`, `administrador_laboratorio`, `estudiante` | `usuarios`                |
| `tipo_mantenimiento` | `correctivo`, `preventivo`                                                              | `detalles_mantenimientos` |

The backend maps PostgreSQL enums with `PgName` and `NpgsqlDataSourceBuilder.MapEnum<T>()`.

`schema.sql` is the reference for fresh databases, not an upgrade script. Existing databases must match that schema before deploying this source. No EF migrations or `update.sql` are used. Schema changes for populated databases require explicit, reviewed SQL. Restart application processes after enum changes.

`codigo_ucb` is nullable and unique when present. `prestamos.autorizado_por`, `entregado_por` and `motivo_rechazo` retain actor names and rejection context. Legacy location/provenance text is preserved for historical SQL routines but is not updated by the application; new reads/writes use catalog foreign keys. Contracts store sanitized HTML that can contain identity photos and signatures. Backups must remain in restricted private storage and must never be attached to a release.

## Derived Data

| Data                        | Source of truth                 | Maintenance mechanism                          |
| --------------------------- | ------------------------------- | ---------------------------------------------- |
| Equipment group quantity    | Active `equipos` by group       | Database triggers and repository recalculation |
| Average equipment cost      | Active `equipos` by group       | Database triggers and repository recalculation |
| Furniture locker count      | Active `gaveteros` by furniture | Database triggers and repository recalculation |
| Loan detail deletion        | Parent loan soft delete         | Logical cascade                                |
| Maintenance detail deletion | Parent maintenance soft delete  | Logical cascade                                |

Derived values exist to speed up administrative screens. Business logic should still validate critical decisions, especially availability, at the service layer.

`usuarios.imagen_perfil`, `usuarios.imagen_frente_carnet`, `usuarios.imagen_atras_carnet`, `usuarios.imagen_firma`, `configuraciones_sistema.firma_jefe_carrera_base64`, and final contract HTML use ASP.NET Core Data Protection payloads rather than raw sensitive content. The key ring persists across deployments in restricted infrastructure. Final contracts retain their own immutable protected copy so later profile or institutional-signature changes do not alter historical records.

`usuarios.email_verificado` blocks local authentication until ownership is confirmed. `token_verificacion_hash` stores SHA-256 output rather than the emailed token and is cleared atomically after confirmation. `google_id` uniquely links an institutional Google identity. `codigos_autenticacion` stores only hashes of ten-minute OAuth exchange codes and removes consumed or expired rows as new codes are issued.

## Availability and Duration Rules

An equipment unit is unavailable when an overlapping loan is `aprobado`, `activo`, or `atrasado`, or when an overlapping maintenance record includes that unit. Availability is checked again inside a serializable transaction when a reservation is created.

`grupos_equipos.tiempo_max_prestamo_dias` is the single source of truth for maximum loan duration. The allowed interval is 1 to 365 days. A request containing multiple groups uses the most restrictive maximum, and both availability queries and reservation creation validate the exact timestamp duration rather than calendar-day boundaries.

## Indexes

| Index                                           | Purpose                                   |
| ----------------------------------------------- | ----------------------------------------- |
| `idx_usuarios_email_estado`                     | Login and email lookup.                   |
| `idx_usuarios_nombre_estado`                    | User listing and search.                  |
| `idx_prestamos_temporal_usuario_estado`         | Loan history and date filters.            |
| `idx_mantenimientos_temporal_empresa_estado`    | Maintenance history by provider and date. |
| `idx_grupos_equipos_busqueda`                   | Equipment group catalog search.           |
| `idx_equipos_agrupacion`                        | Joins between physical units and groups.  |
| `idx_detalles_prestamos_por_prestamo`           | Loan detail lookups.                      |
| `idx_detalles_mantenimientos_por_mantenimiento` | Maintenance detail lookups.               |

## Views

| View                                 | Purpose                                                  |
| ------------------------------------ | -------------------------------------------------------- |
| `vw_equipos_necesitan_mantenimiento` | Lists equipment that may require preventive maintenance. |
| `vw_ubicaciones_grupos_equipos`      | Exposes physical location by furniture and locker.       |

## Initialize Database

```bash
set -a; . code/server.env; set +a
createdb -U "$POSTGRES_USER" "$POSTGRES_DB"
psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "\dt"
```

With Docker:

```bash
cd code
docker compose --env-file server.env up -d ucb_db
```

Docker Compose mounts `schema.sql` into the PostgreSQL init directory, so an empty persistent volume is initialized with structure and Row Level Security on its first start, without copying user data. An existing volume is never re-initialized.

## Generate and Publish the Empty Schema

Generate the schema from Git Bash on Windows:

```bash
export PATH="/c/Program Files/PostgreSQL/17/bin:$PATH"
pg_dump -h localhost -U "$POSTGRES_USER" -d "$POSTGRES_DB" --schema-only --no-owner --no-privileges --file code/database/schema.sql.tmp
mv code/database/schema.sql.tmp code/database/schema.sql
```

Verify that it has structure and no table data:

```bash
test -s code/database/schema.sql
! grep -Eq '^-- Data for Name:|^COPY public\.|^SELECT pg_catalog\.setval' code/database/schema.sql
```

Append the `Row Level Security` section of the previous `schema.sql` before the closing comment, then upload it to the release:

```bash
gh release upload <tag> code/database/schema.sql --repo ucbscz/hold --clobber
```

Initialize an empty database:

```bash
psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1 -f code/database/schema.sql
```

## Row Level Security

The API must connect with a dedicated role that is not a superuser, does not own the tables and has `NOBYPASSRLS`. Outside Development the server refuses to start if the connected role bypasses RLS or if the Row Level Security section of `code/database/schema.sql` was not applied.

```bash
psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v app_user="$APP_DB_USER" -v app_password="$APP_DB_PASSWORD" <<'SQL'
ALTER ROLE :"app_user" WITH LOGIN PASSWORD :'app_password';
SQL
```

`schema.sql` creates the application role without login and applies the policies; `APP_DB_USER` must match it. On a database that already has data, apply only its idempotent RLS section:

```bash
sed -n '/^-- Row Level Security/,$p' code/database/schema.sql | psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1
```

Then set `ConnectionStrings__PostgreSQL` to use the application role (`APP_DB_USER`).

Each EF Core connection publishes `app.carnet` and `app.rol`; policies use them:

| Table | Regular user | Anonymous endpoints | Administrators and system |
| --- | --- | --- | --- |
| `usuarios` | Read; update own row | Read, register, update (login, verification, recovery) | Full access |
| `prestamos` | Read; create and update own loans | Read | Full access |
| `contratos` | Read contracts of own loans | None | Full access |
| `notificaciones`, `avisos_disponibilidad` | Own rows | None | Full access |
| `audit_logs` | None | None | Full access |
| `codigos_autenticacion` | None | Full access | System only |

Writes performed on behalf of the system (audit entries, notifications, contract rendering, availability watches) run inside `ApplicationDbContext.RunAsSystemAsync()`. Sessions without context (`app.rol` unset) see no rows.

Do not upload `.backup`, full SQL dumps, Data Protection keys, environment files, or user documents to GitHub releases. Store encrypted operational backups in private Oracle storage with access logging and a tested restore procedure.
