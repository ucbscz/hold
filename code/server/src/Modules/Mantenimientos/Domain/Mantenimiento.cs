using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class Mantenimiento : Entity
{
    public int IdEmpresa { get; set; }
    public DateTime FechaMantenimiento { get; set; }
    public DateTime FechaFinalMantenimiento { get; set; }
    public string? Descripcion { get; set; }
    public double? Costo { get; set; }
}
