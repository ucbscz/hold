namespace IMT_Reservas.Server.Application.Features.Configuracion;

public class ConfiguracionDto
{
    public decimal MontoMinimoContrato { get; set; }
    public int HorarioInicioMinutos { get; set; }
    public int HorarioFinMinutos { get; set; }
    public List<HorarioAtencionDto> Horarios { get; set; } = [];
    public string NombreJefeCarrera { get; set; } = string.Empty;
    public string? CarnetJefeCarrera { get; set; }
    public string FirmaJefeCarreraBase64 { get; set; } = string.Empty;
    public int TiempoMinimoReservaMinutos { get; set; }
    public int TiempoRecordatorioPrevioMinutos { get; set; }
    public int MinutosGraciaAtraso { get; set; }
}

public sealed record ConfiguracionPublicaDto
{
    public decimal MontoMinimoContrato { get; init; }
    public int HorarioInicioMinutos { get; init; }
    public int HorarioFinMinutos { get; init; }
    public List<HorarioAtencionDto> Horarios { get; init; } = [];
    public int TiempoMinimoReservaMinutos { get; init; }
    public int TiempoRecordatorioPrevioMinutos { get; init; }
    public int MinutosGraciaAtraso { get; init; }

    public static ConfiguracionPublicaDto From(ConfiguracionDto source) => new()
    {
        MontoMinimoContrato = source.MontoMinimoContrato,
        HorarioInicioMinutos = source.HorarioInicioMinutos,
        HorarioFinMinutos = source.HorarioFinMinutos,
        Horarios = source.Horarios,
        TiempoMinimoReservaMinutos = source.TiempoMinimoReservaMinutos,
        TiempoRecordatorioPrevioMinutos = source.TiempoRecordatorioPrevioMinutos,
        MinutosGraciaAtraso = source.MinutosGraciaAtraso,
    };
}

public sealed record HorarioAtencionDto
{
    public int DiaSemana { get; set; }
    public DateOnly? Fecha { get; set; }
    public bool Abierto { get; set; } = true;
    public int InicioMinutos { get; set; } = 480;
    public int FinMinutos { get; set; } = 1080;
}
