using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Mantenimientos.Domain;

public class DetalleMantenimiento : Entity
{
    public int IdMantenimiento { get; set; }
    public int IdEquipo { get; set; }
    public string? TipoMantenimiento { get; set; }
    public string? Descripcion { get; set; }
}
