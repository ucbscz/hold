using NpgsqlTypes;

namespace IMT_Reservas.Server.Modules.Inventario.Domain;

public enum EstadoEquipo
{
    [PgName(EstadoEquipoNombre.Operativo)]
    Operativo,

    [PgName(EstadoEquipoNombre.ParcialmenteOperativo)]
    ParcialmenteOperativo,

    [PgName(EstadoEquipoNombre.Inoperativo)]
    Inoperativo,

    [PgName(EstadoEquipoNombre.EnMantenimiento)]
    EnMantenimiento,
}
