namespace IMT_Reservas.Server.Application.Features.Mantenimiento;

public class MantenimientoDto
{
    public int? Id { get; set; }
    public int? IdEmpresa { get; set; }
    public string? NombreEmpresaMantenimiento { get; set; }
    public DateTime? FechaMantenimiento { get; set; }
    public DateTime? FechaFinalMantenimiento { get; set; }
    public double? Costo { get; set; }
    public string? Descripcion { get; set; }
    public string? TipoMantenimiento { get; set; }
    public string? NombreGrupoEquipo { get; set; }
    public string? CodigoImtEquipo { get; set; }
    public string? DescripcionEquipo { get; set; }
    public int[]? CodigoImt { get; set; }
    public string[]? TiposMantenimiento { get; set; }
    public string[]? DescripcionesEquipo { get; set; }
}
