namespace IMT_Reservas.Server.Application.Features.Carrito;

public sealed class DisponibilidadCalendarioDto
{
    public DateTime? FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }
    public DateTime? FechaDesde { get; init; }
    public DateTime? FechaHasta { get; init; }
    public List<GrupoCantidadCarritoDto> Grupos { get; init; } = [];
}

public sealed class GrupoCantidadCarritoDto
{
    public int IdGrupoEquipo { get; init; }
    public int Cantidad { get; init; }
}

public sealed class DisponibilidadDiaDto
{
    public DateTime Fecha { get; init; }
    public bool Disponible { get; init; }
}
