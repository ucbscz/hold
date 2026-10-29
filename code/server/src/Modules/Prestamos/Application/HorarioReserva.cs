using IMT_Reservas.Server.Modules.Configuracion.Domain;
using IMT_Reservas.Server.Shared.Configuration;

namespace IMT_Reservas.Server.Modules.Prestamos.Application;

public sealed class HorarioReserva
{
    private readonly RegionClock _clock;

    public HorarioReserva(RegionClock clock) => _clock = clock;

    public string Mensaje =>
        $"Selecciona una fecha y hora dentro del horario de atención autorizado (hora de {_clock.Name})";

    public bool EsValido(DateTime inicio, DateTime fin, ConfiguracionSistema config)
    {
        var inicioLocal = _clock.ToLocal(inicio);
        var finLocal = _clock.ToLocal(fin);
        var mismoDia = inicioLocal.Date == finLocal.Date;

        return fin > inicio
            && DentroHorario(inicioLocal, config, false, mismoDia)
            && DentroHorario(finLocal, config, true, mismoDia);
    }

    private static bool DentroHorario(
        DateTime fecha,
        ConfiguracionSistema config,
        bool devolucion,
        bool mismoDia
    )
    {
        var horario = config.Horarios.FirstOrDefault(h => h.Fecha == DateOnly.FromDateTime(fecha))
            ?? config.Horarios.FirstOrDefault(h => h.Fecha == null && h.DiaSemana == (int)fecha.DayOfWeek);
        var abierto = horario?.Abierto ?? fecha.DayOfWeek != DayOfWeek.Sunday;
        var inicio = horario?.InicioMinutos ?? config.HorarioInicioMinutos;
        var fin = horario?.FinMinutos ?? config.HorarioFinMinutos;
        var minutos = fecha.TimeOfDay.TotalMinutes;
        var limiteInicio = mismoDia ? fin - config.TiempoMinimoReservaMinutos : fin;
        return abierto
            && minutos >= inicio
            && (devolucion ? minutos <= fin : mismoDia ? minutos <= limiteInicio : minutos < limiteInicio);
    }

    public bool MismoDia(DateTime inicio, DateTime fin) =>
        _clock.ToLocal(inicio).Date == _clock.ToLocal(fin).Date;
}
