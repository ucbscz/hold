namespace IMT_Reservas.Server.Application.Features.Usuario;

public class UsuarioDto
{
    public string? Carnet { get; set; }
    public string? Nombre { get; set; }
    public string? ApellidoPaterno { get; set; }
    public string? ApellidoMaterno { get; set; }
    public string? Rol { get; set; }
    public string? Email { get; set; }
    public string? Contrasena { get; set; }
    public string? CarreraNombre { get; set; }
    public int? IdCarrera { get; set; }
    public string? Telefono { get; set; }
    public bool? Bloqueado { get; set; }
    public string? MotivoBloqueo { get; set; }
    public string? NombreReferencia { get; set; }
    public string? TelefonoReferencia { get; set; }
    public string? EmailReferencia { get; set; }
    public byte[]? ImagenFrenteCarnet { get; set; }
    public byte[]? ImagenAtrasCarnet { get; set; }
    public byte[]? ImagenFirma { get; set; }
    public bool? AceptaTerminos { get; set; }
    public bool? EmailVerificado { get; set; }
    public string? CodigoGoogle { get; set; }
    public bool? EmailVerificacionEnviada { get; set; }
}
