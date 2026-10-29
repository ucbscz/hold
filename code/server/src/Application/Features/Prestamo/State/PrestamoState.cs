using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Application.Features.Prestamo.State;

public static class PrestamoState
{
    public const string TodosFiltro = "todos";

    private static readonly Dictionary<EstadoPrestamo, EstadoPrestamoBase> States = new()
    {
        [EstadoPrestamo.Pendiente] = new EstadoPendiente(),
        [EstadoPrestamo.Aprobado] = new EstadoAprobado(),
        [EstadoPrestamo.Activo] = new EstadoActivo(),
        [EstadoPrestamo.Rechazado] = new EstadoRechazado(),
        [EstadoPrestamo.Finalizado] = new EstadoFinalizado(),
        [EstadoPrestamo.Cancelado] = new EstadoCancelado(),
        [EstadoPrestamo.Atrasado] = new EstadoAtrasado(),
    };

    private static readonly Dictionary<string, EstadoPrestamo> ParseMap = States.ToDictionary(
        kv => kv.Value.Nombre,
        kv => kv.Key
    );

    public static bool CanTransition(EstadoPrestamo current, EstadoPrestamo next) =>
        States.TryGetValue(current, out var state) && state.CanTransitionTo(next);

    public static EstadoPrestamo? Parse(string? estado) =>
        estado != null && ParseMap.TryGetValue(estado.ToLowerInvariant(), out var e) ? e : null;

    public static string ToText(EstadoPrestamo estado) => States[estado].Nombre;
}
