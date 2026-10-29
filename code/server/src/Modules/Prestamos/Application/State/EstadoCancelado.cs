using UCB_Hold.Server.Modules.Prestamos.Domain;

namespace UCB_Hold.Server.Modules.Prestamos.Application.State;

public sealed class EstadoCancelado : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Cancelado;
    public override string Nombre => "cancelado";
    protected override EstadoPrestamo[] AllowedTransitions => [];
}
