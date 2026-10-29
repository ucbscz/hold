using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Prestamos.Domain;

public class AvisoDisponibilidad : Entity
{
    public string CarnetUsuario { get; set; } = string.Empty;
    public int IdGrupoEquipo { get; set; }
    public DateTime Fecha { get; set; }
    public int Cantidad { get; set; } = 1;
    public bool Notificado { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
