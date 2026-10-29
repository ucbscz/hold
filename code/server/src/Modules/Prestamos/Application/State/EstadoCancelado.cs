using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Application.Features.Prestamo.State;

public sealed class EstadoCancelado : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Cancelado;
    public override string Nombre => "cancelado";
    protected override EstadoPrestamo[] AllowedTransitions => [];
}
