using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Mantenimientos.Domain;

public class DetalleMantenimiento : Entity
{
    public int IdMantenimiento { get; set; }
    public int IdEquipo { get; set; }
    public string? TipoMantenimiento { get; set; }
    public string? Descripcion { get; set; }
}
