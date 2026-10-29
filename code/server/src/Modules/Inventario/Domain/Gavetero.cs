using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class Gavetero : Entity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public int IdMueble { get; set; }
    public Mueble? Mueble { get; set; }
    public double? Longitud { get; set; }
    public double? Profundidad { get; set; }
    public double? Altura { get; set; }
}
