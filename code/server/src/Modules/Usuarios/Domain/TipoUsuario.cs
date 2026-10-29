using NpgsqlTypes;

namespace UCB_Hold.Server.Modules.Usuarios.Domain;

public enum TipoUsuario
{
    [PgName("docente")]
    Docente,

    [PgName("administrador")]
    Administrador,

    [PgName("estudiante")]
    Estudiante,

    [PgName("administrativo")]
    Administrativo,

    [PgName("administrador_laboratorio")]
    Administrador_Laboratorio,
}
