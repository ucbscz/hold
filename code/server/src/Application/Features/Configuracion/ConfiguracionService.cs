using Ardalis.Result;
using FluentValidation;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Application.Features.Configuracion;

public class ConfiguracionService
{
    private readonly ConfiguracionRepository _repository;
    private readonly IValidator<ConfiguracionDto> _validator;
    private readonly IMapper<ConfiguracionSistema, ConfiguracionDto> _mapper;

    public ConfiguracionService(
        ConfiguracionRepository repository,
        IValidator<ConfiguracionDto> validator,
        IMapper<ConfiguracionSistema, ConfiguracionDto> mapper)
    {
        _repository = repository;
        _validator = validator;
        _mapper = mapper;
    }

    public async Task<ConfiguracionDto> GetConfiguracion()
    {
        var config = await _repository.GetConfiguracion();
        return _mapper.ToDto(config);
    }

    public async Task<Result<ConfiguracionDto>> UpdateConfiguracion(ConfiguracionDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return validationResult.ToResult<ConfiguracionDto>();

        var config = await _repository.GetConfiguracion();
        _mapper.ToEntity(dto);

        await _repository.Update(config);
        await _repository.InvalidateCache();

        return Result<ConfiguracionDto>.Success(_mapper.ToDto(config));
    }
}
