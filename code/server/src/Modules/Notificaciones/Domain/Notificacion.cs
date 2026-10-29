using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

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
