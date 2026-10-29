using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Notificaciones.Domain;

public class Notificacion : Entity
{
    public string CarnetUsuario { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Contenido { get; set; }
    public string? Detalle { get; set; }
    public bool Leido { get; set; }
    public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
}
