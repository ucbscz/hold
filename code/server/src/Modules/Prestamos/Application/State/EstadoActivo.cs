using UCB_Hold.Server.Modules.Prestamos.Domain;

namespace UCB_Hold.Server.Modules.Prestamos.Application.State;

public sealed class EstadoActivo : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Activo;
    public override string Nombre => "activo";
    protected override EstadoPrestamo[] AllowedTransitions =>
        [EstadoPrestamo.Finalizado, EstadoPrestamo.Cancelado, EstadoPrestamo.Atrasado];
}
