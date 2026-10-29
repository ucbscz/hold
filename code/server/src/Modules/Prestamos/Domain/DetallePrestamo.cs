using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class DetallePrestamo : Entity
{
    public int IdPrestamo { get; set; }
    public int? IdEquipo { get; set; }
    public int IdGrupoEquipo { get; set; }
    public EstadoEquipo? EstadoEquipoRetorno { get; set; }
}
