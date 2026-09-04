namespace IMT_Reservas.Server.Application.Features.Usuario;

public sealed class TokenDto
{
    public string Token { get; set; } = string.Empty;
}

public sealed class EmailDto
{
    public string Email { get; set; } = string.Empty;
}

public sealed class GoogleAuthDto
{
    public bool RequiereRegistro { get; set; }
    public string? CodigoRegistro { get; set; }
    public string? Email { get; set; }
    public string? Nombre { get; set; }
    public string? ApellidoPaterno { get; set; }
    public string? ApellidoMaterno { get; set; }
    public LoginDto? Sesion { get; set; }
}
