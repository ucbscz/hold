using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Mantenimientos.Domain;

public class Mantenimiento : Entity
{
    public int IdEmpresa { get; set; }
    public DateTime FechaMantenimiento { get; set; }
    public DateTime FechaFinalMantenimiento { get; set; }
    public string? Descripcion { get; set; }
    public double? Costo { get; set; }
}
