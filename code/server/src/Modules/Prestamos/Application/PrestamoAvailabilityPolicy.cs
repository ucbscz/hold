using IMT_Reservas.Server.Modules.Prestamos.Domain;

namespace IMT_Reservas.Server.Modules.Prestamos.Application;

public static class PrestamoAvailabilityPolicy
{
    public static readonly EstadoPrestamo[] BlockingStates =
    [
        EstadoPrestamo.Aprobado,
        EstadoPrestamo.Activo,
        EstadoPrestamo.Atrasado,
    ];
}
