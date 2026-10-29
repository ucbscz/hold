namespace IMT_Reservas.Server.Application.Features.Mueble;

public class MuebleDto
{
    public int? Id { get; set; }
    public string? Nombre { get; set; }
    public int? NumeroGaveteros { get; set; }
    public string? Ubicacion { get; set; }
    public int? IdAmbiente { get; set; }
    public string? NombreAmbiente { get; set; }
    public string? Tipo { get; set; }
    public double? Costo { get; set; }
    public double? Longitud { get; set; }
    public double? Profundidad { get; set; }
    public double? Altura { get; set; }
}
