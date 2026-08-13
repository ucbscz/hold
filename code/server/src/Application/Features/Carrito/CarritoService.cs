using Ardalis.Result;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Application.Features.Carrito;

public class CarritoService
{
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

        if (fechaFin <= fechaInicio)
            return Result<List<CarritoDto>>.Error(
                "La fecha y hora final debe ser posterior a la fecha y hora inicial"
            );

        if (fechaFin - fechaInicio < TimeSpan.FromMinutes(30))
            return Result<List<CarritoDto>>.Error(
                "La duracion minima de un prestamo es de 30 minutos"
            );

        var cantidades = await _repository.GetCantidadesByGrupos(request.ArrayIds);
        var prestamosActivos = await _repository.GetPrestamosActivosEnRango(
            request.ArrayIds,
            fechaInicio,
            fechaFin
        );
        var mantenimientosActivos = await _repository.GetMantenimientosActivosEnRango(
            request.ArrayIds,
            fechaInicio,
            fechaFin
        );

        var response = new List<CarritoDto>();

        foreach (var grupoId in request.ArrayIds.Distinct())
        {
            var total = cantidades.TryGetValue(grupoId, out var t) ? t : 0;

            var ocupados = prestamosActivos.Count(p =>
                p.IdGrupoEquipo == grupoId
                && p.FechaPrestamo < fechaFin
                && p.FechaDevolucion > fechaInicio
            ) + mantenimientosActivos.Count(m =>
                m.IdGrupoEquipo == grupoId
                && m.FechaInicio < fechaFin
                && m.FechaFin > fechaInicio
            );

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
