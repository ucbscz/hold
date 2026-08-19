# API Reference

The backend exposes explicit, lowercase REST endpoints under `/api` and returns normalized response objects built with `Ardalis.Result`. Multiword resources use kebab case, such as `/api/grupo-equipo`.

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

Common statuses:

| Status             | Meaning                                      |
| ------------------ | -------------------------------------------- |
| `200 OK`           | Request completed successfully.              |
| `201 Created`      | Resource created successfully.               |
| `400 Bad Request`  | Validation or business-rule failure.         |
| `401 Unauthorized` | Missing or invalid credentials.              |
| `403 Forbidden`    | Authenticated user does not have permission. |
| `404 Not Found`    | Resource does not exist or is not visible.   |

## Authentication

JWT is used for authenticated requests. Protected endpoints require:

```http
Authorization: Bearer <token>
```

Administrative operations require an administrator account.

## Endpoints

### Usuario

| Method   | Route                   | Purpose                               |
| -------- | ----------------------- | ------------------------------------- |
| `GET`    | `/api/usuario`          | List active users.                    |
| `GET`    | `/api/usuario/{carnet}` | Get a user by carnet.                 |
| `POST`   | `/api/usuario`          | Create a user and hash the password.  |
| `PUT`    | `/api/usuario/{carnet}` | Update user data.                     |
| `DELETE` | `/api/usuario/{carnet}` | Soft-delete a user.                   |
| `POST`   | `/api/usuario/login`    | Authenticate and return session data. |

### GrupoEquipo

| Method   | Route                                | Purpose                               |
| -------- | ------------------------------------ | ------------------------------------- |
| `GET`    | `/api/grupo-equipo`                  | List active equipment groups.         |
| `GET`    | `/api/grupo-equipo/{id}`             | Get an equipment group by id.         |
| `POST`   | `/api/grupo-equipo`                  | Create an equipment group.            |
| `PUT`    | `/api/grupo-equipo/{id}`             | Update an equipment group.            |
| `DELETE` | `/api/grupo-equipo/{id}`             | Soft-delete an equipment group.       |
| `GET`    | `/api/grupo-equipo/{id}/comentarios` | List comments for an equipment group. |
| `POST`   | `/api/grupo-equipo/{id}/comentarios` | Add an authenticated comment.         |

### Equipo

| Method   | Route              | Purpose                                |
| -------- | ------------------ | -------------------------------------- |
| `GET`    | `/api/equipo`      | List active physical equipment units.  |
| `GET`    | `/api/equipo/{id}` | Get an equipment unit by id.           |
| `POST`   | `/api/equipo`      | Create a physical equipment unit.      |
| `PUT`    | `/api/equipo/{id}` | Update a physical equipment unit.      |
| `DELETE` | `/api/equipo/{id}` | Soft-delete a physical equipment unit. |

### Prestamo

| Method   | Route                                | Purpose                                |
| -------- | ------------------------------------ | -------------------------------------- |
| `GET`    | `/api/prestamo`                      | List loans for administration.         |
| `GET`    | `/api/prestamo/{id}`                 | Get a loan by id.                      |
| `GET`    | `/api/prestamo/historial`            | Get loan history for a user.           |
| `GET`    | `/api/prestamo/por-usuario/{carnet}` | Get a user's loans for administration. |
| `POST`   | `/api/prestamo`                      | Create a loan request.                 |
| `PUT`    | `/api/prestamo/{id}/estado`          | Change loan state.                     |
| `DELETE` | `/api/prestamo/{id}`                 | Soft-delete a loan.                    |

### Disponibilidad

| Method | Route                                 | Purpose                                            |
| ------ | ------------------------------------- | -------------------------------------------------- |
| `POST` | `/api/carrito/disponibilidad-equipos` | Calculate available units by group and date range. |

### Mantenimiento

| Method   | Route                     | Purpose                           |
| -------- | ------------------------- | --------------------------------- |
| `GET`    | `/api/mantenimiento`      | List maintenance records.         |
| `GET`    | `/api/mantenimiento/{id}` | Get a maintenance record.         |
| `POST`   | `/api/mantenimiento`      | Create a maintenance record.      |
| `PUT`    | `/api/mantenimiento/{id}` | Update a maintenance record.      |
| `DELETE` | `/api/mantenimiento/{id}` | Soft-delete a maintenance record. |

### Catalogs

| Controller             | Base route                   | Operations |
| ---------------------- | ---------------------------- | ---------- |
| `Categoria`            | `/api/categoria`             | CRUD       |
| `Carrera`              | `/api/carrera`               | CRUD       |
| `Accesorio`            | `/api/accesorio`             | CRUD       |
| `Componente`           | `/api/componente`            | CRUD       |
| `EmpresaMantenimiento` | `/api/empresa-mantenimiento` | CRUD       |
| `Mueble`               | `/api/mueble`                | CRUD       |
| `Gavetero`             | `/api/gavetero`              | CRUD       |

## Business Rules

| Area         | Rule                                                                                       |
| ------------ | ------------------------------------------------------------------------------------------ |
| Users        | `Carnet` and `Email` are required and unique. `Telefono` is unique when provided.          |
| Passwords    | Minimum 8 characters, at least one uppercase letter, one number and one special character. |
| Equipment    | `CodigoImt` is assigned when the unit is created and must not be changed later.            |
| Loans        | User, equipment group, loan date and return date are required.                             |
| Availability | Only loans in `aprobado` or `activo` state block capacity.                                 |
| Approval     | Availability is revalidated before a pending loan can be approved.                         |

Loan state model:

```text
pendiente -> aprobado -> activo -> finalizado
    |             |
    |             +-> cancelado
    +-> rechazado
```

## Health Check

```http
GET /api/health
```

Returns `200 OK` with `Healthy` when the API and database are available.
