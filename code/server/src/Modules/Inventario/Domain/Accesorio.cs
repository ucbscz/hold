using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Domain;

public class Accesorio : Entity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Modelo { get; set; } = string.Empty;
    public string? UrlDataSheet { get; set; }
    public double? Precio { get; set; }
    public int IdEquipo { get; set; }
    public string? Tipo { get; set; }
}
