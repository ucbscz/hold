using UCB_Hold.Server.Modules.Prestamos.Domain;

namespace UCB_Hold.Server.Modules.Prestamos.Application.State;

public sealed class EstadoFinalizado : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Finalizado;
    public override string Nombre => "finalizado";
    protected override EstadoPrestamo[] AllowedTransitions => [];
}
