using IMT_Reservas.Server.Modules.Usuarios.Application;

namespace IMT_Reservas.Server.Modules.Usuarios.Api;

public sealed class AuthSessionResponseDto
{
    public UsuarioDto Usuario { get; init; } = new();
}

public sealed class GoogleAuthResponseDto
{
    public bool RequiereRegistro { get; init; }
    public string? CodigoRegistro { get; init; }
    public string? Email { get; init; }
    public string? Nombre { get; init; }
    public string? ApellidoPaterno { get; init; }
    public string? ApellidoMaterno { get; init; }
    public AuthSessionResponseDto? Sesion { get; init; }
}
