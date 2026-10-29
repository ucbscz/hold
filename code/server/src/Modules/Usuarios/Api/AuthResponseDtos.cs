using IMT_Reservas.Server.Application.Features.Usuario;

namespace IMT_Reservas.Server.Presentation.Controllers.Implementations;

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
