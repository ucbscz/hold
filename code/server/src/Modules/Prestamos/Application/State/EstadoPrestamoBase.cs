using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Application.Features.Prestamo.State;

public abstract class EstadoPrestamoBase
{
    public abstract EstadoPrestamo Valor { get; }
    public abstract string Nombre { get; }
    protected abstract EstadoPrestamo[] AllowedTransitions { get; }

    public bool CanTransitionTo(EstadoPrestamo next) => AllowedTransitions.Contains(next);
}
