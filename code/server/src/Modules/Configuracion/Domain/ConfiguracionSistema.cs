using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Configuracion.Domain;

public class ConfiguracionSistema : Entity
{
    public decimal MontoMinimoContrato { get; set; }
    public int HorarioInicioMinutos { get; set; }
    public int HorarioFinMinutos { get; set; }
    public List<HorarioAtencion> Horarios { get; set; } = [];
    public string NombreJefeCarrera { get; set; } = null!;
    public string? CarnetJefeCarrera { get; set; }
    public string FirmaJefeCarreraBase64 { get; set; } = null!;
    public int TiempoMinimoReservaMinutos { get; set; }
    public int TiempoRecordatorioPrevioMinutos { get; set; }
    public int MinutosGraciaAtraso { get; set; }
}
