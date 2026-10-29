namespace IMT_Reservas.Server.Application.Features.Equipo;

public class HistorialEquipoDto
{
    public int? IdPrestamo { get; set; }
    public string? Carnet { get; set; }
    public string? NombreUsuario { get; set; }
    public DateTime? FechaPrestamo { get; set; }
    public DateTime? FechaDevolucionEsperada { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public string? EstadoPrestamo { get; set; }
    public string? EstadoEquipo { get; set; }
    public string? Observacion { get; set; }
}
