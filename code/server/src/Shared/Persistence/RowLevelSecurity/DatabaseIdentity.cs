using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace IMT_Reservas.Server.Shared.Persistence.RowLevelSecurity;

public sealed record DatabaseIdentity(string Carnet, string Rol)
{
    public const string SystemRole = "sistema";
    public const string AnonymousRole = "anonimo";
    public const string AuthenticatedRole = "usuario";

    public static DatabaseIdentity From(HttpContext? context)
    {
        if (context == null)
            return new(string.Empty, SystemRole);

        var identity = context.User.Identity as ClaimsIdentity;
        var carnet = identity?.IsAuthenticated == true ? identity.Name ?? string.Empty : string.Empty;
        var allowsAnonymous =
            context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null;

        if (carnet.Length == 0 || allowsAnonymous)
            return new(carnet, AnonymousRole);

        var rol = identity!.FindFirst(identity.RoleClaimType)?.Value;
        return new(carnet, string.IsNullOrWhiteSpace(rol) ? AuthenticatedRole : rol);
    }

    public DatabaseIdentity AsSystem() => this with { Rol = SystemRole };
}
