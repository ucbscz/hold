namespace IMT_Reservas.Server.Application.Features.Configuracion;

public class ConfiguracionDto
{
    public decimal MontoMinimoContrato { get; set; }
    public int HorarioInicioMinutos { get; set; }
    public int HorarioFinMinutos { get; set; }
    public List<Core.Entities.HorarioAtencion> Horarios { get; set; } = [];
    public string NombreJefeCarrera { get; set; } = string.Empty;
    public string? CarnetJefeCarrera { get; set; }
    public string FirmaJefeCarreraBase64 { get; set; } = string.Empty;
    public int TiempoMinimoReservaMinutos { get; set; }
    public int TiempoRecordatorioPrevioMinutos { get; set; }
    public int MinutosGraciaAtraso { get; set; }
}
