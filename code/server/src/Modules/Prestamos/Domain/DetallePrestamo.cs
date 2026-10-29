using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Prestamos.Domain;

public class DetallePrestamo : Entity
{
    public int IdPrestamo { get; set; }
    public int? IdEquipo { get; set; }
    public int IdGrupoEquipo { get; set; }
    public EstadoEquipo? EstadoEquipoRetorno { get; set; }
}
