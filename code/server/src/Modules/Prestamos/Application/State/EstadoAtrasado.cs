using IMT_Reservas.Server.Modules.Prestamos.Domain;

namespace IMT_Reservas.Server.Modules.Prestamos.Application.State;

public sealed class EstadoAtrasado : EstadoPrestamoBase
{
    public override EstadoPrestamo Valor => EstadoPrestamo.Atrasado;
    public override string Nombre => "atrasado";
    protected override EstadoPrestamo[] AllowedTransitions => [EstadoPrestamo.Finalizado];
}
