using FluentValidation;
using Ardalis.Result;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Application.Features.Configuracion;

public class ConfiguracionService
{
    private readonly ConfiguracionRepository _repository;
    private readonly ConfiguracionValidator _validator;
    private readonly ConfiguracionMapper _mapper;

    public ConfiguracionService(
        ConfiguracionRepository repository,
        ConfiguracionValidator validator,
        ConfiguracionMapper mapper)
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
        {
            return Result<ConfiguracionDto>.Error(validationResult.Errors.First().ErrorMessage);
        }

        var config = await _repository.GetConfiguracion();
        _mapper.UpdateEntity(dto, config);
        
        await _repository.Update(config);
        await _repository.InvalidateCache();

        return Result<ConfiguracionDto>.Success(_mapper.ToDto(config));
    }
}
