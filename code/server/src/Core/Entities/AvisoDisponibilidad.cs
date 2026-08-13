using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class AvisoDisponibilidad : Entity
{
    public string CarnetUsuario { get; set; } = string.Empty;
    public int IdGrupoEquipo { get; set; }
    public DateTime Fecha { get; set; }
    public int Cantidad { get; set; } = 1;
    public bool Notificado { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
