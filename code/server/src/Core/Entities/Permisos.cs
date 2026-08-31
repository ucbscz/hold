using System.Security.Claims;

namespace IMT_Reservas.Server.Core.Entities;

public static class Permisos
{
    public const string Gestion = "administrador,administrador_laboratorio";
    public static bool PuedeGestionar(this ClaimsPrincipal user) =>
        user.IsInRole("administrador") || user.IsInRole("administrador_laboratorio");
}
