using IMT_Reservas.Server.Modules.Prestamos.Domain;

namespace IMT_Reservas.Server.Modules.Prestamos.Application.State;

public sealed class EstadoFinalizado : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Finalizado;
    public override string Nombre => "finalizado";
    protected override EstadoPrestamo[] AllowedTransitions => [];
}
