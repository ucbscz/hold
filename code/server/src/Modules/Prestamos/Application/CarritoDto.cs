namespace IMT_Reservas.Server.Application.Features.Carrito;

public class CarritoDto
{
    public DateTime? Fecha { get; init; }
    public int? IdGrupoEquipo { get; init; }
    public int? CantidadDisponible { get; init; }
    public int? TotalOperativo { get; init; }
    public DateTime? FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }
    public List<int>? ArrayIds { get; init; } = new();
}
