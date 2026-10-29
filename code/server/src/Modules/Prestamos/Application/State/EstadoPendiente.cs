using IMT_Reservas.Server.Modules.Prestamos.Domain;

namespace IMT_Reservas.Server.Modules.Prestamos.Application.State;

public sealed class EstadoPendiente : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Pendiente;
    public override string Nombre => "pendiente";
    protected override EstadoPrestamo[] AllowedTransitions =>
        [EstadoPrestamo.Aprobado, EstadoPrestamo.Rechazado, EstadoPrestamo.Cancelado];
}
