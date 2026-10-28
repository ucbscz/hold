using System.Security.Claims;

namespace IMT_Reservas.Server.Core.Entities;

public static class Permisos
{
    public const string Administrador = "administrador";
    public const string AdministradorLaboratorio = "administrador_laboratorio";
    public const string Gestion = Administrador + "," + AdministradorLaboratorio;

    public static bool PuedeGestionar(this ClaimsPrincipal user) =>
        user.IsInRole(Administrador) || user.IsInRole(AdministradorLaboratorio);
}
