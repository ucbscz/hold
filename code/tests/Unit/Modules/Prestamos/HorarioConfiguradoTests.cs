using FluentAssertions;
using UCB_Hold.Server.Modules.Configuracion.Domain;
using UCB_Hold.Server.Modules.Prestamos.Application;
using UCB_Hold.Tests.Helpers;

namespace UCB_Hold.Tests.Unit.Modules.Prestamos;

[TestFixture]
public class HorarioConfiguradoTests
{
    private static readonly HorarioReserva Horario = TestBranding.Horario;

    private static ConfiguracionSistema Config() => new()
    {
        HorarioInicioMinutos = 480,
        HorarioFinMinutos = 1080,
        TiempoMinimoReservaMinutos = 30
    };

    [Test]
    public void WeeklySchedule_ReplacesDefaultHours()
    {
        var config = Config();
        config.Horarios.Add(new() { DiaSemana = 1, InicioMinutos = 600, FinMinutos = 960 });
        Horario.EsValido(new(2026, 8, 31, 9, 0, 0), new(2026, 8, 31, 10, 0, 0), config).Should().BeFalse();
        Horario.EsValido(new(2026, 8, 31, 15, 30, 0), new(2026, 8, 31, 16, 0, 0), config).Should().BeTrue();
        Horario.EsValido(new(2026, 8, 31, 15, 59, 0), new(2026, 9, 1, 8, 30, 0), config).Should().BeTrue();
        Horario.EsValido(new(2026, 8, 31, 16, 0, 0), new(2026, 9, 1, 8, 30, 0), config).Should().BeFalse();
    }

    [Test]
    public void DateOverride_WinsOverWeeklySchedule()
    {
        var config = Config();
        config.Horarios.Add(new() { DiaSemana = 1, InicioMinutos = 480, FinMinutos = 1080 });
        config.Horarios.Add(new() { Fecha = new DateOnly(2026, 8, 31), Abierto = false });
        Horario.EsValido(new(2026, 8, 31, 9, 0, 0), new(2026, 8, 31, 9, 30, 0), config).Should().BeFalse();
    }

    [Test]
    public void Sunday_DefaultClosedButCanBeExplicitlyOpened()
    {
        var config = Config();
        var inicio = new DateTime(2026, 8, 30, 13, 0, 0, DateTimeKind.Utc);
        Horario.EsValido(inicio, inicio.AddMinutes(30), config).Should().BeFalse();
        config.Horarios.Add(new() { DiaSemana = 0, InicioMinutos = 540, FinMinutos = 720 });
        Horario.EsValido(inicio, inicio.AddMinutes(30), config).Should().BeTrue();
    }

    [Test]
    public void InternalDate_UsesBoliviaRatherThanUtcMidnight()
    {
        Horario.MismoDia(new(2026, 8, 31, 23, 0, 0, DateTimeKind.Utc), new(2026, 9, 1, 1, 0, 0, DateTimeKind.Utc)).Should().BeTrue();
    }

    [Test]
    public void MultiDayLoan_AllowsClosedDaysBetweenValidEndpoints()
    {
        var config = Config();

        Horario.EsValido(
            new(2026, 9, 5, 17, 45, 0),
            new(2026, 9, 7, 8, 30, 0),
            config
        ).Should().BeTrue();
    }
}
