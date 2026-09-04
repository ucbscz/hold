using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Application.Features.Carrito;

public class CarritoService
{
    private const int MaxGroupsPerRequest = 100;
    private readonly CarritoRepository _repository;
    private readonly ILogger<CarritoService> _logger;
    private readonly ConfiguracionRepository _configuracionRepository;

    public CarritoService(
        CarritoRepository repository,
        ILogger<CarritoService> logger,
        ConfiguracionRepository configuracionRepository
    )
    {
        _repository = repository;
        _logger = logger;
        _configuracionRepository = configuracionRepository;
    }

    public async Task<Result<List<CarritoDto>>> GetDisponibilidad(
        CarritoDto request,
        CancellationToken cancellationToken = default
    )
    {
        if (
            request.FechaInicio == null
            || request.FechaFin == null
            || request.ArrayIds == null
            || request.ArrayIds.Count == 0
        )
        {
            _logger.LogWarning(
                "Disponibilidad request missing fields or empty IDs: Inicio={Inicio}, Fin={Fin}, IdsCount={IdsCount}",
                request.FechaInicio,
                request.FechaFin,
                request.ArrayIds?.Count
            );

            return Result<List<CarritoDto>>.Success([]);
        }

        var fechaInicio = request.FechaInicio.Value;
        var fechaFin = request.FechaFin.Value;
        var groupIds = request.ArrayIds.Distinct().ToList();

        if (groupIds.Count > MaxGroupsPerRequest)
            return Result<List<CarritoDto>>.Error(
                $"No se pueden consultar más de {MaxGroupsPerRequest} grupos a la vez"
            );

        if (fechaFin <= fechaInicio)
            return Result<List<CarritoDto>>.Error(
                "La fecha y hora final debe ser posterior a la fecha y hora inicial"
            );

        var config = await _configuracionRepository.GetConfiguracion();
        if (fechaFin - fechaInicio < TimeSpan.FromMinutes(config.TiempoMinimoReservaMinutos))
            return Result<List<CarritoDto>>.Error(
                $"La duracion minima de un prestamo es de {config.TiempoMinimoReservaMinutos} minutos"
            );

        if (!HorarioReserva.EsValido(fechaInicio, fechaFin, config))
            return Result<List<CarritoDto>>.Error(
                HorarioReserva.Mensaje
            );

        var limits = await _repository.GetLoanLimitsByGroups(groupIds, cancellationToken);
        var duration = fechaFin - fechaInicio;
        var exceeded = limits.Values
            .Where(limit => duration > TimeSpan.FromDays(limit.MaximoDias))
            .OrderBy(limit => limit.MaximoDias)
            .FirstOrDefault();

        if (exceeded != default)
            return Result<List<CarritoDto>>.Error(
                $"El grupo '{exceeded.Nombre}' permite préstamos de hasta {exceeded.MaximoDias} día(s)"
            );

        var cantidades = await _repository.GetCantidadesByGrupos(groupIds, cancellationToken);
        var prestamosActivos = await _repository.GetPrestamosActivosEnRango(
            groupIds,
            fechaInicio,
            fechaFin,
            cancellationToken
        );
        var mantenimientosActivos = await _repository.GetMantenimientosActivosEnRango(
            groupIds,
            fechaInicio,
            fechaFin,
            cancellationToken
        );
        var unavailableByGroup = prestamosActivos
            .Concat(mantenimientosActivos)
            .GroupBy(item => item.IdGrupoEquipo)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.IdEquipo).Distinct().Count()
            );

        var response = new List<CarritoDto>();

        foreach (var grupoId in groupIds)
        {
            var total = cantidades.TryGetValue(grupoId, out var t) ? t : 0;

            var ocupados = unavailableByGroup.GetValueOrDefault(grupoId);

            response.Add(
                new CarritoDto
                {
                    Fecha = fechaInicio,
                    IdGrupoEquipo = grupoId,
                    CantidadDisponible = Math.Max(0, total - ocupados),
                    TotalOperativo = total,
                }
            );
        }

        return Result<List<CarritoDto>>.Success(response);
    }

    public async Task<Result<List<DisponibilidadDiaDto>>> GetDisponibilidadCalendario(
        DisponibilidadCalendarioDto request,
        CancellationToken cancellationToken = default
    )
    {
        if (
            request.FechaInicio == null
            || request.FechaFin == null
            || request.FechaDesde == null
            || request.FechaHasta == null
            || request.Grupos.Count == 0
        )
            return Result<List<DisponibilidadDiaDto>>.Invalid();

        var fechaInicio = request.FechaInicio.Value;
        var fechaFin = request.FechaFin.Value;
        var fechaDesde = request.FechaDesde.Value.Date;
        var fechaHasta = request.FechaHasta.Value.Date;
        if (fechaFin <= fechaInicio || fechaHasta < fechaDesde || (fechaHasta - fechaDesde).TotalDays > 42)
            return Result<List<DisponibilidadDiaDto>>.Invalid();

        var cantidadesSolicitadas = request.Grupos
            .Where(group => group.IdGrupoEquipo > 0 && group.Cantidad > 0)
            .GroupBy(group => group.IdGrupoEquipo)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Cantidad));
        if (cantidadesSolicitadas.Count == 0 || cantidadesSolicitadas.Count > MaxGroupsPerRequest)
            return Result<List<DisponibilidadDiaDto>>.Invalid();

        var ids = cantidadesSolicitadas.Keys.ToList();
        var duracion = fechaFin - fechaInicio;
        var inicioCobertura = CombinarFechaYHora(fechaDesde, fechaInicio);
        var finCobertura = CombinarFechaYHora(fechaHasta, fechaInicio).Add(duracion);
        var totales = await _repository.GetCantidadesByGrupos(ids, cancellationToken);
        var ocupados = await _repository.GetEquiposOcupadosEnRango(
            ids,
            inicioCobertura,
            finCobertura,
            cancellationToken
        );

        var resultado = new List<DisponibilidadDiaDto>();
        for (var fecha = fechaDesde; fecha <= fechaHasta; fecha = fecha.AddDays(1))
        {
            var inicio = CombinarFechaYHora(fecha, fechaInicio);
            var fin = inicio.Add(duracion);
            var disponible = cantidadesSolicitadas.All(group =>
            {
                var ocupadosDelGrupo = ocupados
                    .Where(item =>
                        item.IdGrupoEquipo == group.Key
                        && item.FechaInicio < fin
                        && item.FechaFin > inicio
                    )
                    .Select(item => item.IdEquipo)
                    .Distinct()
                    .Count();
                return (totales.GetValueOrDefault(group.Key) - ocupadosDelGrupo) >= group.Value;
            });
            resultado.Add(new DisponibilidadDiaDto { Fecha = fecha, Disponible = disponible });
        }

        return Result<List<DisponibilidadDiaDto>>.Success(resultado);
    }

    private static DateTime CombinarFechaYHora(DateTime fecha, DateTime hora) =>
        new(fecha.Year, fecha.Month, fecha.Day, hora.Hour, hora.Minute, hora.Second, DateTimeKind.Utc);
}
