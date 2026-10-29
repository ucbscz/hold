using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Application.Features.Prestamo.State;

public sealed class EstadoPendiente : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Pendiente;
    public override string Nombre => "pendiente";
    protected override EstadoPrestamo[] AllowedTransitions =>
        [EstadoPrestamo.Aprobado, EstadoPrestamo.Rechazado, EstadoPrestamo.Cancelado];
}
