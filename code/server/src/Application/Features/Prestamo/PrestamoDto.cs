namespace IMT_Reservas.Server.Application.Features.Prestamo;

public class PrestamoDto
{
    public int? Id { get; set; }
    public string? CarnetUsuario { get; set; }
    public string? NombreUsuario { get; set; }
    public string? ApellidoPaternoUsuario { get; set; }
    public string? TelefonoUsuario { get; set; }
    public DateTime? FechaSolicitud { get; set; }
    public DateTime? FechaPrestamoEsperada { get; set; }
    public DateTime? FechaPrestamo { get; set; }
    public DateTime? FechaDevolucionEsperada { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public string? Observacion { get; set; }
    public string? EstadoPrestamo { get; set; }
    public int? IdContrato { get; set; }
    public string? NombreGrupoEquipo { get; set; }
    public string? CodigoImt { get; set; }
    public string? UbicacionEquipo { get; set; }
    public string? NombreGavetero { get; set; }
    public string? NombreMueble { get; set; }
    public string? UbicacionMueble { get; set; }
    public List<int>? GrupoEquipoId { get; set; }
    public string? Contrato { get; set; }
    public string? EstadoEquipo { get; set; }
    public List<PrestamoDto>? EquiposRetorno { get; set; }
    public string DestinoPrestamo { get; set; } = "Universidad";
    public int? IdCarrera { get; set; }
    public string? NombreMateria { get; set; }
    public string? TipoUsuario { get; set; }
}
