using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Application.Features.Prestamo.State;

public sealed class EstadoAtrasado : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Atrasado;
    public override string Nombre => "atrasado";
    protected override EstadoPrestamo[] AllowedTransitions => [EstadoPrestamo.Finalizado];
}
