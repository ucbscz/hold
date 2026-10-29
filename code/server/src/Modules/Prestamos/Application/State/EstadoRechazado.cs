using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Application.Features.Prestamo.State;

public sealed class EstadoRechazado : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Rechazado;
    public override string Nombre => "rechazado";
    protected override EstadoPrestamo[] AllowedTransitions => [];
}
