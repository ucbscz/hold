using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Application.Features.Prestamo.State;

public sealed class EstadoAprobado : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Aprobado;
    public override string Nombre => "aprobado";
    protected override EstadoPrestamo[] AllowedTransitions =>
        [EstadoPrestamo.Activo, EstadoPrestamo.Cancelado];
}
