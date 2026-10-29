using IMT_Reservas.Server.Modules.Inventario.Domain;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Prestamos.Domain;

public class DetallePrestamo : Entity
{
    public int IdPrestamo { get; set; }
    public int? IdEquipo { get; set; }
    public int IdGrupoEquipo { get; set; }
    public EstadoEquipo? EstadoEquipoRetorno { get; set; }
}
