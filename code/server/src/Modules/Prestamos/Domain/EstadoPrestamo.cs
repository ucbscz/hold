using NpgsqlTypes;

namespace IMT_Reservas.Server.Modules.Prestamos.Domain;

public enum EstadoPrestamo
{
    [PgName("pendiente")]
    Pendiente,

    [PgName("aprobado")]
    Aprobado,

    [PgName("activo")]
    Activo,

    [PgName("rechazado")]
    Rechazado,

    [PgName("finalizado")]
    Finalizado,

    [PgName("cancelado")]
    Cancelado,

    [PgName("atrasado")]
    Atrasado,
}
