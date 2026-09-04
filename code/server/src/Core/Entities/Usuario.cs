using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class Usuario : Entity
{
    public string Carnet { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string ApellidoMaterno { get; set; } = string.Empty;
    public TipoUsuario Rol { get; set; } = TipoUsuario.Estudiante;
    public string Contrasena { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? TelefonoReferencia { get; set; }
    public string? NombreReferencia { get; set; }
    public string? EmailReferencia { get; set; }
    public int IdCarrera { get; set; }
    public bool Bloqueado { get; set; }
    public string? MotivoBloqueo { get; set; }
    public byte[]? ImagenFrenteCarnet { get; set; }
    public byte[]? ImagenAtrasCarnet { get; set; }
    public byte[]? ImagenFirma { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public bool EmailVerificado { get; set; } = true;
    public string? GoogleId { get; set; }
    public string? TokenVerificacionHash { get; set; }
    public DateTime? TokenVerificacionExpira { get; set; }
}
