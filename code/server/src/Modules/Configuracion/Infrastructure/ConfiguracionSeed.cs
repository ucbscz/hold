using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Infrastructure.Config;

public static class ConfiguracionSeed
{
    public static ConfiguracionSistema Default => new()
    {
        Id = 1,
        MontoMinimoContrato = 2000,
        HorarioInicioMinutos = 480,
        HorarioFinMinutos = 1080,
        TiempoMinimoReservaMinutos = 30,
        TiempoRecordatorioPrevioMinutos = 30,
        MinutosGraciaAtraso = 15,
        NombreJefeCarrera = string.Empty,
        FirmaJefeCarreraBase64 = string.Empty,
    };
}
