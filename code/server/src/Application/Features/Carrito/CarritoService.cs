using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Application.Features.Carrito;

public class CarritoService
{
    private const int MaxGroupsPerRequest = 100;
    private readonly CarritoRepository _repository;
    private readonly ILogger<CarritoService> _logger;

    public CarritoService(CarritoRepository repository, ILogger<CarritoService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<CarritoDto>>> GetDisponibilidad(CarritoDto request)
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

        if (fechaFin - fechaInicio < TimeSpan.FromMinutes(30))
            return Result<List<CarritoDto>>.Error(
                "La duracion minima de un prestamo es de 30 minutos"
            );

        if (!HorarioReserva.EsValido(fechaInicio, fechaFin))
            return Result<List<CarritoDto>>.Error(HorarioReserva.Mensaje);

        var limits = await _repository.GetLoanLimitsByGroups(groupIds);
        var duration = fechaFin - fechaInicio;
        var exceeded = limits.Values
            .Where(limit => duration > TimeSpan.FromDays(limit.MaximoDias))
            .OrderBy(limit => limit.MaximoDias)
            .FirstOrDefault();

        if (exceeded != default)
            return Result<List<CarritoDto>>.Error(
                $"El grupo '{exceeded.Nombre}' permite préstamos de hasta {exceeded.MaximoDias} día(s)"
            );

        var cantidades = await _repository.GetCantidadesByGrupos(groupIds);
        var prestamosActivos = await _repository.GetPrestamosActivosEnRango(
            groupIds,
            fechaInicio,
            fechaFin
        );
        var mantenimientosActivos = await _repository.GetMantenimientosActivosEnRango(
            groupIds,
            fechaInicio,
            fechaFin
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
}
