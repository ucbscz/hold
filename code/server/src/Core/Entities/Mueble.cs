using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class Mueble : Entity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public string? Ubicacion { get; set; }
    public int? IdAmbiente { get; set; }
    public Ambiente? Ambiente { get; set; }
    public int NumeroGaveteros { get; set; }
    public double? Longitud { get; set; }
    public double? Profundidad { get; set; }
    public double? Altura { get; set; }
    public double? Costo { get; set; }
}
