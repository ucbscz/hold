namespace IMT_Reservas.Server.Application.Features.Componente;

public class ComponenteDto
{
    public int? Id { get; set; }
    public string? Nombre { get; set; }
    public string? Modelo { get; set; }
    public string? Tipo { get; set; }
    public string? Descripcion { get; set; }
    public double? PrecioReferencia { get; set; }
    public int? IdEquipo { get; set; }
    public string? NombreEquipo { get; set; }
    public string? CodigoImtEquipo { get; set; }
    public string? UrlDataSheet { get; set; }
}
