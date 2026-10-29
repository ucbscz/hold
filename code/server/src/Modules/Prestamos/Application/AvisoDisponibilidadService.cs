using System.Globalization;
using System.Text.Json;
using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Notificacion;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Application.Features.AvisoDisponibilidad;

public class AvisoDisponibilidadService
{
    private readonly AvisoDisponibilidadRepository _repository;
    private readonly ConfiguracionRepository _configRepo;
    private readonly PrestamoDisponibilidadRepository _availability;
    private readonly NotificacionService _notifications;

    public AvisoDisponibilidadService(
        AvisoDisponibilidadRepository repository,
        ConfiguracionRepository configRepo,
        PrestamoDisponibilidadRepository availability,
        NotificacionService notifications
    )
    {
        _repository = repository;
        _configRepo = configRepo;
        _availability = availability;
        _notifications = notifications;
    }

    public async Task<Result<object>> Create(string carnet, AvisoDisponibilidadDto dto)
    {
        if ((dto.IdGrupoEquipo ?? 0) <= 0 || dto.Fecha == null)
            return Result<object>.Error("Grupo y fecha/hora requeridos");

        if (dto.Fecha <= DateTime.UtcNow)
            return Result<object>.Error("La fecha y hora del aviso debe ser futura");

        var config = await _configRepo.GetConfiguracion();
        if (!HorarioReserva.EsValido(dto.Fecha.Value, MinimumEnd(dto.Fecha.Value, config), config))
            return Result<object>.Error(HorarioReserva.Mensaje);

        await _repository.Add(carnet, dto);

        return Result<object>.Success(null!);
    }

    public async Task NotifyAvailable(
        ConfiguracionSistema config,
        CancellationToken cancellationToken = default
    )
    {
        var pending = await _repository.GetPending();
        var available = new List<Core.Entities.AvisoDisponibilidad>();
        var checkedSlots = new Dictionary<(int Grupo, DateTime Fecha), bool>();

        foreach (var watch in pending)
        {
            var end = MinimumEnd(watch.Fecha, config);
            if (!HorarioReserva.EsValido(watch.Fecha, end, config))
                continue;

            var slot = (watch.IdGrupoEquipo, watch.Fecha);
            if (!checkedSlots.TryGetValue(slot, out var isAvailable))
            {
                isAvailable = await _availability.HasAvailableEquipo(
                    watch.IdGrupoEquipo,
                    watch.Fecha,
                    end,
                    cancellationToken
                );
                checkedSlots[slot] = isAvailable;
            }

            if (isAvailable)
                available.Add(watch);
        }

        if (available.Count == 0)
            return;

        await _notifications.CreateMany(available.Select(BuildNotification).ToList());
        await _repository.MarkAsNotified(available.Select(watch => watch.Id).ToList());
    }

    private static DateTime MinimumEnd(DateTime start, ConfiguracionSistema config) =>
        start.AddMinutes(config.TiempoMinimoReservaMinutos);

    private static NotificacionDto BuildNotification(Core.Entities.AvisoDisponibilidad watch) =>
        new()
        {
            CarnetUsuario = watch.CarnetUsuario,
            Tipo = nameof(TipoNotificacion.DisponibilidadLiberada),
            Titulo = "Disponibilidad liberada",
            Contenido =
                $"Un equipo que esperabas está disponible el {watch.Fecha.ToString(NotificacionService.DateFormat, CultureInfo.InvariantCulture)}.",
            Detalle = JsonSerializer.Serialize(
                new
                {
                    emisor = NotificacionService.SystemEmitter,
                    grupoEquipoId = watch.IdGrupoEquipo,
                    fecha = watch.Fecha.ToString(NotificacionService.DateFormat, CultureInfo.InvariantCulture),
                }
            ),
        };
}
