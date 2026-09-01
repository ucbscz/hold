using NpgsqlTypes;

namespace IMT_Reservas.Server.Core.Entities;

public enum EstadoEquipo
{
    [PgName("operativo")]
    Operativo,

    [PgName("parcialmente_operativo")]
    ParcialmenteOperativo,

    [PgName("inoperativo")]
    Inoperativo,

    [PgName("en_mantenimiento")]
    EnMantenimiento,
}
