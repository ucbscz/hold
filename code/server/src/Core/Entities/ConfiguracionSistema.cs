using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class ConfiguracionSistema : Entity
{
    public decimal MontoMinimoContrato { get; set; }
    public int HorarioInicioMinutos { get; set; }
    public int HorarioFinMinutos { get; set; }
    public List<HorarioAtencion> Horarios { get; set; } = [];
    public string NombreJefeCarrera { get; set; } = null!;
    public string FirmaJefeCarreraBase64 { get; set; } = null!;
    public int TiempoMinimoReservaMinutos { get; set; }
    public int TiempoRecordatorioPrevioMinutos { get; set; }
    public int MinutosGraciaAtraso { get; set; }
}
