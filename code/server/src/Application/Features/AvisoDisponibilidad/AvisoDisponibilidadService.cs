using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Configuracion;
using IMT_Reservas.Server.Application.Features.Prestamo;

namespace IMT_Reservas.Server.Application.Features.AvisoDisponibilidad;

public class AvisoDisponibilidadService
{
    private readonly IAvisoDisponibilidadRepository _repository;
    private readonly IConfiguracionRepository _configRepo;

    public AvisoDisponibilidadService(IAvisoDisponibilidadRepository repository, IConfiguracionRepository configRepo)
    {
        _repository = repository;
        _configRepo = configRepo;
    }

    public async Task<Result<object>> Create(string carnet, AvisoDisponibilidadDto dto)
    {
        if ((dto.IdGrupoEquipo ?? 0) <= 0 || dto.Fecha == null)
            return Result<object>.Error("Grupo y fecha/hora requeridos");

        if (dto.Fecha <= DateTime.UtcNow)
            return Result<object>.Error("La fecha y hora del aviso debe ser futura");

        var config = await _configRepo.GetConfiguracion();
        if (!HorarioReserva.EsValido(dto.Fecha.Value, dto.Fecha.Value.AddMinutes(config.TiempoMinimoReservaMinutos), config.HorarioInicioMinutos, config.HorarioFinMinutos))
            return Result<object>.Error(HorarioReserva.Mensaje);

        await _repository.Add(carnet, dto);

        return Result<object>.Success(null!);
    }
}
