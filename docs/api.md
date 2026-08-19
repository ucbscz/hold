# API Reference

The API is a REST contract under `/api`. Collection resources use lowercase plural nouns, multiword resources use kebab case, and the current user's cart is the only singleton resource. Examples: `/api/equipos`, `/api/grupos-equipos`, and `/api/carrito`.

## Conventions

- JSON property names retain the backend DTO contract.
- Collection queries use `GET`; creation uses `POST`; full updates use `PUT`; and deletion uses `DELETE`.
- Nested resources describe ownership, for example `/api/grupos-equipos/{id}/comentarios`.
- Routes are lowercase. The former `/Objeto/:id` client route and earlier singular API routes are not supported.
- Protected routes require a Bearer token. Administrator-only operations additionally require the `administrador` role.

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

| Method | Route                   | Purpose                                    |
| ------ | ----------------------- | ------------------------------------------ |
| `POST` | `/api/usuarios`         | Register a user. Public.                   |
| `POST` | `/api/usuarios/login`   | Authenticate and create a session. Public. |
| `POST` | `/api/usuarios/refresh` | Refresh an access token. Public.           |

## Users and Notifications

| Method   | Route                            | Purpose                                        |
| -------- | -------------------------------- | ---------------------------------------------- |
| `GET`    | `/api/usuarios`                  | List users. Administrator only.                |
| `GET`    | `/api/usuarios/{carnet}`         | Get a user by carnet.                          |
| `PUT`    | `/api/usuarios/{carnet}`         | Update a user.                                 |
| `PUT`    | `/api/usuarios/{carnet}/bloqueo` | Block or unblock a user. Administrator only.   |
| `DELETE` | `/api/usuarios/{carnet}`         | Soft-delete a user. Administrator only.        |
| `GET`    | `/api/notificaciones`            | List notifications for the authenticated user. |
| `PUT`    | `/api/notificaciones/{id}/leido` | Mark one notification as read.                 |
| `PUT`    | `/api/notificaciones/leidos`     | Mark all notifications as read.                |

## Equipment Catalog

All CRUD operations for the following resources use `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, and `DELETE /{id}`. Write operations are administrator-only.

| Resource              | Base route                    |
| --------------------- | ----------------------------- |
| Categories            | `/api/categorias`             |
| Careers               | `/api/carreras`               |
| Accessories           | `/api/accesorios`             |
| Components            | `/api/componentes`            |
| Maintenance companies | `/api/empresas-mantenimiento` |
| Furniture             | `/api/muebles`                |
| Lockers               | `/api/gaveteros`              |
| Equipment units       | `/api/equipos`                |
| Equipment groups      | `/api/grupos-equipos`         |
| Maintenance records   | `/api/mantenimientos`         |

Additional catalog routes:

| Method | Route                                    | Purpose                                               |
| ------ | ---------------------------------------- | ----------------------------------------------------- |
| `GET`  | `/api/equipos/por-grupo/{grupoId}`       | List equipment units in a group. Administrator only.  |
| `GET`  | `/api/equipos/por-gavetero/{gaveteroId}` | List equipment units in a locker. Administrator only. |
| `GET`  | `/api/equipos/{id}/historial`            | Get equipment loan history. Administrator only.       |
| `GET`  | `/api/gaveteros/por-mueble/{muebleId}`   | List lockers in a furniture item. Administrator only. |
| `GET`  | `/api/grupos-equipos/buscar`             | Search equipment groups by `nombre` and `categoria`.  |

## Comments

| Method   | Route                                                      | Purpose                                                |
| -------- | ---------------------------------------------------------- | ------------------------------------------------------ |
| `GET`    | `/api/grupos-equipos/{id}/comentarios`                     | List group comments. Supports `orden`.                 |
| `POST`   | `/api/grupos-equipos/{id}/comentarios`                     | Add an authenticated comment or reply.                 |
| `POST`   | `/api/grupos-equipos/{id}/comentarios/{comentarioId}/like` | Toggle the authenticated user's like.                  |
| `DELETE` | `/api/grupos-equipos/{id}/comentarios/{comentarioId}`      | Delete an own comment or any comment as administrator. |

## Loans, Availability, and Contracts

| Method   | Route                                  | Purpose                                                           |
| -------- | -------------------------------------- | ----------------------------------------------------------------- |
| `GET`    | `/api/prestamos`                       | List loans. Administrator only.                                   |
| `GET`    | `/api/prestamos/{id}`                  | Get a loan.                                                       |
| `POST`   | `/api/prestamos`                       | Create a loan request.                                            |
| `PUT`    | `/api/prestamos/{id}`                  | Update a loan.                                                    |
| `PUT`    | `/api/prestamos/{id}/estado`           | Change a loan state. Administrator only.                          |
| `DELETE` | `/api/prestamos/{id}`                  | Soft-delete a loan.                                               |
| `GET`    | `/api/prestamos/estado-reserva`        | Get reservation status for the authenticated user.                |
| `GET`    | `/api/prestamos/historial`             | Get user loan history using `carnetUsuario` and `estadoPrestamo`. |
| `GET`    | `/api/prestamos/por-usuario/{carnet}`  | Get a user's loans for administration.                            |
| `GET`    | `/api/prestamos/contrato/{prestamoId}` | Get a loan contract.                                              |
| `POST`   | `/api/contratos/crear`                 | Upload or create a contract for a loan. Multipart form data.      |
| `GET`    | `/api/contratos/{prestamoId}`          | Get a contract by loan id.                                        |
| `DELETE` | `/api/contratos/{prestamoId}`          | Delete a contract.                                                |
| `POST`   | `/api/carrito/disponibilidad-equipos`  | Calculate availability for an equipment group and date range.     |
| `POST`   | `/api/avisos-disponibilidad`           | Create an availability watch for the authenticated user.          |

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
