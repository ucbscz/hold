using UCB_Hold.Server.Modules.Usuarios.Domain;
using UCB_Hold.Server.Shared.Security;

namespace UCB_Hold.Server.Modules.Usuarios.Application;

public static class ManagementRoles
{
    public static bool Includes(TipoUsuario rol) =>
        rol is TipoUsuario.Administrador or TipoUsuario.Administrador_Laboratorio;

    public static bool Includes(string? rol) =>
        rol is Permisos.Administrador or Permisos.AdministradorLaboratorio;
}
