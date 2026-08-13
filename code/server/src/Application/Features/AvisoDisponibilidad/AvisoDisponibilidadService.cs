using Ardalis.Result;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Application.Features.AvisoDisponibilidad;

public class AvisoDisponibilidadService
{
    private readonly AvisoDisponibilidadRepository _repository;

    public AvisoDisponibilidadService(AvisoDisponibilidadRepository repository) =>
        _repository = repository;

    public async Task<Result<object>> Create(string carnet, AvisoDisponibilidadDto dto)
    {
        if ((dto.IdGrupoEquipo ?? 0) <= 0 || dto.Fecha == null)
            return Result<object>.Error("Grupo y fecha/hora requeridos");

        if (dto.Fecha <= DateTime.UtcNow)
            return Result<object>.Error("La fecha y hora del aviso debe ser futura");

        await _repository.Add(carnet, dto);

        return Result<object>.Success(null!);
    }
}
