using UCB_Hold.Server.Modules.Prestamos.Domain;

namespace UCB_Hold.Server.Modules.Prestamos.Application;

public static class PrestamoAvailabilityPolicy
{
    public static readonly EstadoPrestamo[] BlockingStates =
    [
        EstadoPrestamo.Aprobado,
        EstadoPrestamo.Activo,
        EstadoPrestamo.Atrasado,
    ];
}
