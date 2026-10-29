using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Application.Features.Prestamo.State;

public sealed class EstadoActivo : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Activo;
    public override string Nombre => "activo";
    protected override EstadoPrestamo[] AllowedTransitions =>
        [EstadoPrestamo.Finalizado, EstadoPrestamo.Cancelado, EstadoPrestamo.Atrasado];
}
