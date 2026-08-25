using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class Prestamo : Entity
{
    public DateTime FechaSolicitud { get; set; }
    public DateTime FechaPrestamoEsperada { get; set; }
    public DateTime? FechaPrestamo { get; set; }
    public DateTime FechaDevolucionEsperada { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public string? Carnet { get; set; } = string.Empty;
    public EstadoPrestamo EstadoPrestamo { get; set; }
    public string? Observacion { get; set; }
    public int? IdContrato { get; set; }
    public bool RecordatorioEnviado { get; set; } = false;
    public string DestinoPrestamo { get; set; } = "Universidad";
    public int? IdCarrera { get; set; }
    public string? NombreMateria { get; set; }

    public Usuario? Usuario { get; set; }
    public Contrato? Contrato { get; set; }
    public Carrera? Carrera { get; set; }
}
