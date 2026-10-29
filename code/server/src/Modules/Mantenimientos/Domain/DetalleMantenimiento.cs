using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class DetalleMantenimiento : Entity
{
    public int IdMantenimiento { get; set; }
    public int IdEquipo { get; set; }
    public string? TipoMantenimiento { get; set; }
    public string? Descripcion { get; set; }
}
