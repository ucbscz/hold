# API Reference

The API is a REST contract under `/api`. Collection resources use short, lowercase, one-word plural nouns, and the current user's cart is the only singleton resource. Examples: `/api/equipos`, `/api/grupos`, and `/api/carrito`.

## Conventions

- JSON property names retain the backend DTO contract.
- Collection queries use `GET`; creation uses `POST`; full updates use `PUT`; and deletion uses `DELETE`.
- Partial state changes use `PATCH`.
- Nested resources describe ownership, for example `/api/grupos/{id}/comentarios`.
- Filters use query parameters instead of action routes such as `buscar`, `por-grupo`, or `historial`.
- Routes are lowercase. Previous mixed-case client routes and earlier API routes are not supported or redirected.
- Protected routes require a Bearer token. Administrator-only operations additionally require the `administrador` role.

## Client Routes

The Angular application uses lowercase Spanish paths with one-word segments and no hyphens. `/login` is the explicit authentication exception.

| Route             | Purpose                         |
| ----------------- | ------------------------------- |
| `/login`          | Authenticate an existing user.  |
| `/registro`       | Register a new user.            |
| `/inicio`         | Browse the equipment catalog.   |
| `/equipo/{id}`    | View one equipment group.       |
| `/carrito`        | Review and schedule a request.  |
| `/reserva`        | Review and sign the contract.   |
| `/perfil`         | View and edit the user profile. |
| `/historial`      | Review the user's loans.        |
| `/administracion` | Open the administration panel.  |

## Response Contract

Successful responses include a status code and a `Value` payload:

```json
{
  "Status": 200,
  "Value": { "Id": 1, "Nombre": "Osciloscopio" },
  "Errors": [],
  "ValidationErrors": []
}
```

Validation and domain failures preserve the same structure:

```json
{
  "Status": 400,
  "Value": null,
  "Errors": ["Carnet ya existe"],
  "ValidationErrors": []
}
```

| Status             | Meaning                                      |
| ------------------ | -------------------------------------------- |
| `200 OK`           | Request completed successfully.              |
| `201 Created`      | Resource created successfully.               |
| `204 No Content`   | Resource deleted successfully.               |
| `400 Bad Request`  | Validation or business-rule failure.         |
| `401 Unauthorized` | Missing or invalid credentials.              |
| `403 Forbidden`    | Authenticated user does not have permission. |
| `404 Not Found`    | Resource does not exist or is not visible.   |

## Authentication

```http
Authorization: Bearer <token>
```

| Method | Route               | Purpose                                    |
| ------ | ------------------- | ------------------------------------------ |
| `POST` | `/api/usuarios`     | Register a user. Public.                   |
| `POST` | `/api/auth/login`   | Authenticate and create a session. Public. |
| `POST` | `/api/auth/refresh` | Rotate the access and refresh tokens.      |

## Users and Notifications

| Method   | Route                              | Purpose                                        |
| -------- | ---------------------------------- | ---------------------------------------------- |
| `GET`    | `/api/usuarios`                    | List users. Administrator only.                |
| `GET`    | `/api/usuarios/{carnet}`           | Get a user by carnet.                          |
| `PUT`    | `/api/usuarios/{carnet}`           | Update a user.                                 |
| `PATCH`  | `/api/usuarios/{carnet}/bloqueo`   | Block or unblock a user. Administrator only.   |
| `DELETE` | `/api/usuarios/{carnet}`           | Soft-delete a user. Administrator only.        |
| `GET`    | `/api/notificaciones`              | List notifications for the authenticated user. |
| `PATCH`  | `/api/notificaciones/{id}/lectura` | Mark one notification as read.                 |
| `PATCH`  | `/api/notificaciones/lectura`      | Mark all notifications as read.                |

Notification details show `Emisor`: the full name of the administrator who performed the action, or `Sistema` when it was generated automatically. Block and unblock operations notify the affected user.

## Equipment Catalog

All CRUD operations for the following resources use `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, and `DELETE /{id}`. Write operations are administrator-only.

| Resource              | Base route            |
| --------------------- | --------------------- |
| Categories            | `/api/categorias`     |
| Careers               | `/api/carreras`       |
| Accessories           | `/api/accesorios`     |
| Components            | `/api/componentes`    |
| Maintenance companies | `/api/empresas`       |
| Furniture             | `/api/muebles`        |
| Lockers               | `/api/gaveteros`      |
| Equipment units       | `/api/equipos`        |
| Equipment groups      | `/api/grupos`         |
| Maintenance records   | `/api/mantenimientos` |

Additional catalog routes:

| Method | Route                            | Purpose                                         |
| ------ | -------------------------------- | ----------------------------------------------- |
| `GET`  | `/api/equipos?grupoId={id}`      | List equipment units in a group.                |
| `GET`  | `/api/equipos?gaveteroId={id}`   | List equipment units in a locker.               |
| `GET`  | `/api/equipos/{id}/prestamos`    | Get equipment loan records. Administrator only. |
| `GET`  | `/api/gaveteros?muebleId={id}`   | List lockers in a furniture item.               |
| `GET`  | `/api/grupos?nombre=&categoria=` | Search equipment groups.                        |

Equipment group payloads expose `TiempoMaximoPrestamoDias` (1 to 365). This group-level value applies to every physical unit in the group and is required for create and update operations.

## Comments

| Method   | Route                                               | Purpose                                                |
| -------- | --------------------------------------------------- | ------------------------------------------------------ |
| `GET`    | `/api/grupos/{id}/comentarios`                      | List group comments. Supports `orden`.                 |
| `POST`   | `/api/grupos/{id}/comentarios`                      | Add an authenticated comment or reply.                 |
| `POST`   | `/api/grupos/{id}/comentarios/{comentarioId}/likes` | Toggle the authenticated user's like.                  |
| `DELETE` | `/api/grupos/{id}/comentarios/{comentarioId}`       | Delete an own comment or any comment as administrator. |

## Loans, Availability, and Contracts

| Method   | Route                            | Purpose                                                           |
| -------- | -------------------------------- | ----------------------------------------------------------------- |
| `GET`    | `/api/prestamos`                 | List all loans for administrators or own loans for regular users. |
| `GET`    | `/api/prestamos/{id}`            | Get a loan.                                                       |
| `POST`   | `/api/prestamos`                 | Create a loan request.                                            |
| `PATCH`  | `/api/prestamos/{id}/estado`     | Change a loan state. Administrator only.                          |
| `DELETE` | `/api/prestamos/{id}`            | Soft-delete a loan.                                               |
| `GET`    | `/api/prestamos/elegibilidad`    | Get reservation eligibility for the authenticated user.           |
| `GET`    | `/api/prestamos?carnet=&estado=` | Filter loans; non-admin users are restricted to their own carnet. |
| `POST`   | `/api/contratos`                 | Upload or create a contract for a loan. Multipart form data.      |
| `GET`    | `/api/contratos/{prestamoId}`    | Get a contract by loan id.                                        |
| `DELETE` | `/api/contratos/{prestamoId}`    | Delete a contract.                                                |
| `POST`   | `/api/carrito/disponibilidad`    | Calculate availability after validating each group's maximum duration. |
| `POST`   | `/api/avisos`                    | Create an availability watch for the authenticated user.          |

## Audit and Health

| Method | Route            | Purpose                                                                                      |
| ------ | ---------------- | -------------------------------------------------------------------------------------------- |
| `GET`  | `/api/auditoria` | Query audit entries. Administrator only. Supports `entidad`, `carnet`, `desde`, and `hasta`. |
| `GET`  | `/api/health`    | Health check for the API and database.                                                       |

## Business Rules

| Area         | Rule                                                                                                       |
| ------------ | ---------------------------------------------------------------------------------------------------------- |
| Users        | `Carnet` and `Email` are required and unique. `Telefono` is unique when provided.                          |
| Passwords    | Minimum 8 characters, at least one uppercase letter, one number, and one special character.                |
| Equipment    | `CodigoImt` is assigned when the unit is created and cannot be changed later.                              |
| Loans        | Users, equipment groups, start date, and return date are required.                                         |
| Availability | Only loans in `aprobado` or `activo` state block capacity.                                                 |
| Approval     | Availability is revalidated before a pending loan can be approved.                                         |
| Blocking     | A blocked user cannot create a loan because the reservation validation reads the current PostgreSQL state. |

```text
pendiente -> aprobado -> activo -> finalizado
    |             |
    |             +-> cancelado
    +-> rechazado
```
