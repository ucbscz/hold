using System.ComponentModel.DataAnnotations.Schema;
using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class GrupoEquipo : Entity
{
    public string Nombre { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public int IdCategoria { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string UrlImagen { get; set; } = string.Empty;
    public string? UrlDataSheet { get; set; }
    public int Cantidad { get; set; }
    public decimal? CostoPromedio { get; set; }
    public int TiempoMaximoPrestamoDias { get; set; } = 7;

    [ForeignKey("IdCategoria")]
    public Categoria? Categoria { get; set; }
}
