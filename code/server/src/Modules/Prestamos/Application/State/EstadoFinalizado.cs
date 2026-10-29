using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Application.Features.Prestamo.State;

public sealed class EstadoFinalizado : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Finalizado;
    public override string Nombre => "finalizado";
    protected override EstadoPrestamo[] AllowedTransitions => [];
}
