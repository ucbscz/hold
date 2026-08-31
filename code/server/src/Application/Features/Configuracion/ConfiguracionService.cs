using Ardalis.Result;
using ValidationError = Ardalis.Result.ValidationError;
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
        var dto = _mapper.ToDto(config);
        var responsable = await _repository.GetResponsable(config.CarnetJefeCarrera, cancellationToken);
        dto.CarnetJefeCarrera = responsable?.Carnet;
        dto.NombreJefeCarrera = responsable == null ? string.Empty : NombreCompleto(responsable);
        if (dto.NombreJefeCarrera != config.NombreJefeCarrera || responsable == null)
            dto.FirmaJefeCarreraBase64 = string.Empty;
        return dto;
    }

    public async Task<Result<ConfiguracionDto>> UpdateConfiguracion(
        ConfiguracionDto dto,
        CancellationToken cancellationToken = default
    )
    {
        var responsable = await _repository.GetResponsable(dto.CarnetJefeCarrera, cancellationToken);
        if (responsable == null)
            return Result<ConfiguracionDto>.Invalid(new ValidationError
            {
                Identifier = nameof(dto.CarnetJefeCarrera),
                ErrorMessage = "Selecciona un usuario activo como jefe de carrera."
            });
        dto.CarnetJefeCarrera = responsable.Carnet;
        dto.NombreJefeCarrera = NombreCompleto(responsable);
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToResult<ConfiguracionDto>();

        var config = await _repository.GetConfiguracion(cancellationToken);
        if (config.CarnetJefeCarrera != dto.CarnetJefeCarrera
            && (config.CarnetJefeCarrera != null || config.NombreJefeCarrera != dto.NombreJefeCarrera)
            && config.FirmaJefeCarreraBase64 == dto.FirmaJefeCarreraBase64)
            return Result<ConfiguracionDto>.Invalid(new ValidationError
            {
                Identifier = nameof(dto.FirmaJefeCarreraBase64),
                ErrorMessage = "Registra la firma del nuevo jefe de carrera."
            });
        _mapper.UpdateEntity(dto, config);

        await _repository.Update(config, cancellationToken);

        return Result<ConfiguracionDto>.Success(_mapper.ToDto(config));
    }

    public async Task<IReadOnlyList<ResponsableConfiguracionDto>> BuscarResponsables(
        string? buscar, CancellationToken cancellationToken)
    {
        var usuarios = await _repository.BuscarResponsables(buscar, cancellationToken);
        return usuarios.Select(u => new ResponsableConfiguracionDto(u.Carnet, NombreCompleto(u))).ToList();
    }

    private static string NombreCompleto(Core.Entities.Usuario usuario) =>
        string.Join(" ", new[] { usuario.Nombre, usuario.ApellidoPaterno, usuario.ApellidoMaterno }
            .Where(nombre => !string.IsNullOrWhiteSpace(nombre)).Select(nombre => nombre.Trim()));
}
