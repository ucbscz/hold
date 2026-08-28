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
    private readonly ConfiguracionMapper _mapper;

    public ConfiguracionService(
        ConfiguracionRepository repository,
        IValidator<ConfiguracionDto> validator,
        ConfiguracionMapper mapper)
    {
        _repository = repository;
        _validator = validator;
        _mapper = mapper;
    }

    public async Task<ConfiguracionDto> GetConfiguracion(
        CancellationToken cancellationToken = default
    )
    {
        var config = await _repository.GetConfiguracion(cancellationToken);
        return _mapper.ToDto(config);
    }

    public async Task<Result<ConfiguracionDto>> UpdateConfiguracion(
        ConfiguracionDto dto,
        CancellationToken cancellationToken = default
    )
    {
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToResult<ConfiguracionDto>();

        var config = await _repository.GetConfiguracion(cancellationToken);
        _mapper.UpdateEntity(dto, config);

        await _repository.Update(config, cancellationToken);

        return Result<ConfiguracionDto>.Success(_mapper.ToDto(config));
    }
}
