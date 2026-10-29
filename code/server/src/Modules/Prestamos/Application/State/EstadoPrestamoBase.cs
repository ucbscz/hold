using IMT_Reservas.Server.Modules.Prestamos.Domain;

namespace IMT_Reservas.Server.Modules.Prestamos.Application.State;

public abstract class EstadoPrestamoBase
{
    public abstract EstadoPrestamo Valor { get; }
    public abstract string Nombre { get; }
    protected abstract EstadoPrestamo[] AllowedTransitions { get; }

    public bool CanTransitionTo(EstadoPrestamo next) => AllowedTransitions.Contains(next);
}
