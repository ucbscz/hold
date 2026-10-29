using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class Componente : Entity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Modelo { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public double? PrecioReferencia { get; set; }
    public int IdEquipo { get; set; }
    public string? UrlDataSheet { get; set; }
}
