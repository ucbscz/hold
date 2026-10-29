using UCB_Hold.Server.Modules.Prestamos.Domain;

namespace UCB_Hold.Server.Modules.Prestamos.Application.State;

public sealed class EstadoPendiente : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Pendiente;
    public override string Nombre => "pendiente";
    protected override EstadoPrestamo[] AllowedTransitions =>
        [EstadoPrestamo.Aprobado, EstadoPrestamo.Rechazado, EstadoPrestamo.Cancelado];
}
