namespace IMT_Reservas.Server.Application.Features.Accesorio;

public class AccesorioDto
{
    public int? Id { get; set; }
    public string? Nombre { get; set; }
    public string? Modelo { get; set; }
    public string? Tipo { get; set; }
    public string? Descripcion { get; set; }
    public double? Precio { get; set; }
    public string? UrlDataSheet { get; set; }
    public int? IdEquipo { get; set; }
    public string? CodigoImtEquipoAsociado { get; set; }
    public string? NombreEquipoAsociado { get; set; }
}
