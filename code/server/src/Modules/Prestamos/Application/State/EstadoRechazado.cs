using UCB_Hold.Server.Modules.Prestamos.Domain;

namespace UCB_Hold.Server.Modules.Prestamos.Application.State;

public sealed class EstadoRechazado : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Rechazado;
    public override string Nombre => "rechazado";
    protected override EstadoPrestamo[] AllowedTransitions => [];
}
