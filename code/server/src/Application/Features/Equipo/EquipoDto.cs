namespace IMT_Reservas.Server.Application.Features.Equipo;

public class EquipoDto
{
    public int? Id { get; set; }
    public int? CodigoImt { get; set; }
    public string? CodigoUcb { get; set; }
    public string? NumeroSerial { get; set; }
    public string? EstadoEquipo { get; set; }
    public string? Ubicacion { get; set; }
    public double? CostoReferencia { get; set; }
    public string? Descripcion { get; set; }
    public string? Procedencia { get; set; }
    public string? NombreGrupoEquipo { get; set; }
    public int? IdGrupoEquipo { get; set; }
    public string? NombreGavetero { get; set; }
    public int? IdGavetero { get; set; }
    public DateTime? FechaIngresoEquipo { get; set; }
}
