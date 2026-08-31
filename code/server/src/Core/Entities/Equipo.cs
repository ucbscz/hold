using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class Equipo : Entity
{
    public int IdGrupoEquipo { get; set; }
    public GrupoEquipo? GrupoEquipo { get; set; }
    public int CodigoImt { get; set; }
    public int? IdGavetero { get; set; }
    public Gavetero? Gavetero { get; set; }
    public string? CodigoUcb { get; set; }
    public string? NumeroSerial { get; set; }
    public EstadoEquipo EstadoEquipo { get; set; } = EstadoEquipo.Operativo;
    public int? IdAmbiente { get; set; }
    public Ambiente? Ambiente { get; set; }
    public double? CostoReferencia { get; set; }
    public string? Descripcion { get; set; }
    public int? IdProcedencia { get; set; }
    public Procedencia? Procedencia { get; set; }
    public DateOnly FechaIngresoEquipo { get; set; }
}
