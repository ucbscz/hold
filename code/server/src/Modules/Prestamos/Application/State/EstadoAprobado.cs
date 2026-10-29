using UCB_Hold.Server.Modules.Prestamos.Domain;

namespace UCB_Hold.Server.Modules.Prestamos.Application.State;

public sealed class EstadoAprobado : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Aprobado;
    public override string Nombre => "aprobado";
    protected override EstadoPrestamo[] AllowedTransitions =>
        [EstadoPrestamo.Activo, EstadoPrestamo.Cancelado];
}
