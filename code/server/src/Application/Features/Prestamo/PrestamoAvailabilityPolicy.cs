using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Application.Features.Prestamo;

public static class PrestamoAvailabilityPolicy
{
    public static readonly EstadoPrestamo[] BlockingStates =
    [
        EstadoPrestamo.Aprobado,
        EstadoPrestamo.Activo,
        EstadoPrestamo.Atrasado,
    ];
}
