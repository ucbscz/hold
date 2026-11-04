using UCB_Hold.Server.Modules.Prestamos.Domain;

namespace UCB_Hold.Server.Modules.Prestamos.Application.State;

public static class PrestamoState
{
    public const string TodosFiltro = "todos";

    private static readonly Dictionary<EstadoPrestamo, EstadoPrestamoBase> States =
        new EstadoPrestamoBase[]
        {
            new EstadoPendiente(),
            new EstadoAprobado(),
            new EstadoActivo(),
            new EstadoRechazado(),
            new EstadoFinalizado(),
            new EstadoCancelado(),
            new EstadoAtrasado(),
        }.ToDictionary(state => state.Valor);

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
