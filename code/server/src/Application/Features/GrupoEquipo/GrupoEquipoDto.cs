namespace IMT_Reservas.Server.Application.Features.GrupoEquipo;

public class GrupoEquipoDto
{
    public int? Id { get; set; }
    public string? Nombre { get; set; }
    public string? Modelo { get; set; }
    public string? Marca { get; set; }
    public string? Descripcion { get; set; }
    public string? UrlDataSheet { get; set; }
    public string? UrlImagen { get; set; }
    public int? IdCategoria { get; set; }
    public string? NombreCategoria { get; set; }
    public int? Cantidad { get; set; }
    public decimal? CostoPromedio { get; set; }
    public int? TiempoMaximoPrestamoDias { get; set; } = 7;
}
