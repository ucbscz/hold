using Ardalis.Result;
using FluentValidation;
using BCryptLib = BCrypt.Net.BCrypt;
using UsuarioEntity = UCB_Hold.Server.Modules.Usuarios.Domain.Usuario;
using UCB_Hold.Server.Modules.Usuarios.Domain;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Usuarios.Infrastructure;
using UCB_Hold.Server.Shared.Caching;
using UCB_Hold.Server.Shared.Kernel;
using UCB_Hold.Server.Shared.Security;

namespace UCB_Hold.Server.Modules.Usuarios.Application;

public class UsuarioService : Service<UsuarioEntity, UsuarioRepository, UsuarioDto>
{
    private static readonly TimeSpan UsuarioCacheTtl = TimeSpan.FromMinutes(30);
    private readonly UsuarioMapper _mapper;
    private readonly CacheRepository _cacheRepository;
    private readonly UsuarioAuthRepository _authRepository;
    private readonly UsuarioReadRepository _queries;
    private readonly SensitiveDataProtector _sensitiveData;
    private readonly VerificacionCorreoService _emailVerification;
    private readonly CodigoExternoService _codigosExternos;

    public UsuarioService(
        UsuarioRepository repository,
        UsuarioMapper mapper,
        IValidator<UsuarioDto> validator,
        CacheRepository cacheRepository,
        AuditLogService audit,
        UsuarioAuthRepository authRepository,
        UsuarioReadRepository queries,
        SensitiveDataProtector sensitiveData,
        VerificacionCorreoService emailVerification,
        CodigoExternoService codigosExternos
    )
        : base(repository, validator, mapper, audit)
    {
        _mapper = mapper;
        _cacheRepository = cacheRepository;
        _authRepository = authRepository;
        _queries = queries;
        _sensitiveData = sensitiveData;
        _emailVerification = emailVerification;
        _codigosExternos = codigosExternos;
    }

    public Task<Result<List<UsuarioDto>>> GetAll(
        int page,
        int pageSize,
        CancellationToken cancellationToken
    ) => Repository.GetPage(page, pageSize, cancellationToken);

    public override async Task<Result<UsuarioDto>> Create(UsuarioDto dto) =>
        await Create(dto, isAdmin: false);

    public async Task<Result<UsuarioDto>> Registrar(
        UsuarioDto dto,
        bool puedeGestionar,
        bool isLabAdmin,
        CancellationToken cancellationToken
    )
    {
        if (!puedeGestionar && string.IsNullOrWhiteSpace(dto.CodigoExterno))
            return Result<UsuarioDto>.Unauthorized(
                "Verifica tu correo institucional para crear tu cuenta"
            );

        return await Create(dto, puedeGestionar, isLabAdmin, cancellationToken);
    }

    public async Task<Result<UsuarioDto>> Create(
        UsuarioDto dto,
        bool isAdmin,
        bool isLabAdmin = false,
        CancellationToken cancellationToken = default
    )
    {
        NormalizeIdentity(dto);
        var registroExterno = await GetRegistroExterno(dto.CodigoExterno, cancellationToken);

        if (!string.IsNullOrWhiteSpace(dto.CodigoExterno) && registroExterno == null)
            return Result<UsuarioDto>.Unauthorized("El registro con la cuenta institucional expiró o ya fue utilizado");

        ApplyRegistroExterno(dto, registroExterno);

        var rejection = RejectRegistration(dto, isAdmin, isLabAdmin);
        if (rejection != null)
            return rejection;

        dto.Bloqueado = false;
        dto.MotivoBloqueo = null;
        dto.EmailVerificado = isAdmin || registroExterno != null;

        await ResolveCarrera(dto, cancellationToken);

        var validation = await Validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
            return validation.ToResult<UsuarioDto>();

        var deletedUser = await Repository.GetDeletedByCarnet(dto.Carnet!, cancellationToken);
        var conflict = await FindConflict(dto, deletedUser, cancellationToken);
        if (conflict != null)
            return conflict;

        ProtectProfileDocuments(dto, null);
        var entity = deletedUser ?? MapToEntity(dto);

        if (deletedUser != null)
            Restore(entity, dto);

        entity.EmailVerificado = isAdmin || registroExterno != null;
        entity.IdentidadExternaId = registroExterno?.IdentidadExternaId;
        entity.TokenVerificacionHash = null;
        entity.TokenVerificacionExpira = null;
        entity.Contrasena = BCryptLib.HashPassword(dto.Contrasena, workFactor: 12);

        var result = await Persist(entity, deletedUser == null, cancellationToken);

        if (!result.IsSuccess || result.Value == null)
            return result;

        return await CompleteRegistration(result, entity, dto, registroExterno, isAdmin);
    }

    private static void NormalizeIdentity(UsuarioDto dto)
    {
        dto.Carnet = dto.Carnet?.Trim().ToUpperInvariant();
        dto.Rol = dto.Rol?.Trim().ToLowerInvariant();
        dto.Email = dto.Email?.Trim().ToLowerInvariant();
    }

    private async Task<CodigoAutenticacion?> GetRegistroExterno(
        string? codigo,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return null;

        var registro = await _codigosExternos.Get(codigo, cancellationToken);
        return registro?.Tipo == TipoCodigoExterno.Registro ? registro : null;
    }

    private static void ApplyRegistroExterno(UsuarioDto dto, CodigoAutenticacion? registro)
    {
        if (registro == null)
            return;

        dto.Email = registro.Email;
        dto.Contrasena = $"Gg1!{AuthTokenGenerator.Create()}";
        dto.EmailVerificado = true;
    }

    private static Result<UsuarioDto>? RejectRegistration(UsuarioDto dto, bool isAdmin, bool isLabAdmin)
    {
        if (string.IsNullOrWhiteSpace(dto.Contrasena))
            return Result<UsuarioDto>.Error("Contraseña requerida");

        if (!isAdmin)
            dto.Rol = null;

        if (!isAdmin && dto.AceptaTerminos != true)
            return Result<UsuarioDto>.Error(
                "Debes aceptar los términos y condiciones para crear tu cuenta"
            );

        return isLabAdmin && ManagementRoles.Includes(dto.Rol) ? Result<UsuarioDto>.Forbidden() : null;
    }

    private async Task<Result<UsuarioDto>?> FindConflict(
        UsuarioDto dto,
        UsuarioEntity? deletedUser,
        CancellationToken cancellationToken
    )
    {
        if (deletedUser == null && await _queries.ExistsByCarnet(dto.Carnet!, cancellationToken))
            return Result<UsuarioDto>.Error("Carnet ya existe");

        if (await _queries.ExistsByEmail(dto.Email!, deletedUser?.Carnet, cancellationToken))
        {
            var existingEmail = await _authRepository.GetTrackedByEmail(dto.Email!, cancellationToken);
            return Result<UsuarioDto>.Error(
                string.IsNullOrWhiteSpace(existingEmail?.IdentidadExternaId)
                    ? "Email ya existe"
                    : "El correo ya está registrado. Inicia sesión con tu cuenta institucional"
            );
        }

        if (
            !string.IsNullOrWhiteSpace(dto.Telefono)
            && await _queries.ExistsByTelefono(dto.Telefono, deletedUser?.Carnet, cancellationToken)
        )
            return Result<UsuarioDto>.Error("Teléfono ya registrado");

        return null;
    }

    private void Restore(UsuarioEntity entity, UsuarioDto dto)
    {
        _mapper.Update(dto, entity);
        entity.EstadoEliminado = false;
        entity.Bloqueado = false;
        entity.MotivoBloqueo = null;
        entity.RefreshToken = null;
        entity.RefreshTokenExpiry = null;
        entity.ImagenPerfil = dto.ImagenPerfil;
        entity.ImagenFrenteCarnet = dto.ImagenFrenteCarnet;
        entity.ImagenAtrasCarnet = dto.ImagenAtrasCarnet;
        entity.ImagenFirma = dto.ImagenFirma;
    }

    private async Task<Result<UsuarioDto>> Persist(
        UsuarioEntity entity,
        bool isNew,
        CancellationToken cancellationToken
    )
    {
        if (isNew)
            return await CreateEntity(entity);

        await Repository.UpdateEntity(entity, cancellationToken: cancellationToken);
        return Result<UsuarioDto>.Success(_mapper.ToDto(entity));
    }

    private async Task<Result<UsuarioDto>> CompleteRegistration(
        Result<UsuarioDto> result,
        UsuarioEntity entity,
        UsuarioDto dto,
        CodigoAutenticacion? registroExterno,
        bool isAdmin
    )
    {
        var created = result.Value!;

        if (registroExterno != null)
        {
            if (!await _codigosExternos.Consume(dto.CodigoExterno!, CancellationToken.None))
                return Result<UsuarioDto>.Conflict("El registro con la cuenta institucional ya fue utilizado");
        }
        else if (!isAdmin)
        {
            created.EmailVerificacionEnviada = await _emailVerification.Issue(
                entity,
                CancellationToken.None
            );
        }

        created.CarreraNombre = await _queries.GetCarreraName(entity.IdCarrera, CancellationToken.None);
        var auditDetail = AuditChangeDetail.BuildCreated(created);
        ProfileDocuments.Clear(created);
        await Audit!.Log(AuditAccion.Crear, typeof(UsuarioEntity).Name, entity.Carnet, auditDetail);

        return result;
    }

    public async Task<Result<UsuarioDto>> Update(
        string carnet,
        UsuarioDto dto,
        string? callerCarnet,
        bool isAdmin = false,
        bool isLabAdmin = false,
        CancellationToken cancellationToken = default
    )
    {
        if (!isAdmin && !string.Equals(callerCarnet, carnet, StringComparison.Ordinal))
            return Result<UsuarioDto>.Forbidden();

        var existing = await Repository.GetTrackedByCarnet(carnet, cancellationToken);

        if (existing == null)
            return Result<UsuarioDto>.NotFound();

        var previous = await ToDtoWithCarrera(existing, cancellationToken);

        if (!isAdmin)
            PreserveTraceableFields(dto, existing);

        dto.Rol = dto.Rol?.Trim().ToLowerInvariant();

        var validation = await Validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
            return validation.ToResult<UsuarioDto>();

        if (isLabAdmin && (ManagementRoles.Includes(existing.Rol) || ManagementRoles.Includes(dto.Rol)))
            return Result<UsuarioDto>.Forbidden();
        dto.Bloqueado = existing.Bloqueado;
        dto.MotivoBloqueo = existing.MotivoBloqueo;

        if (
            !string.IsNullOrWhiteSpace(dto.Telefono)
            && await _queries.ExistsByTelefono(dto.Telefono, carnet, cancellationToken)
        )
            return Result<UsuarioDto>.Error("Teléfono ya registrado");

        await ApplyChanges(dto, existing, isAdmin, cancellationToken);
        await Repository.UpdateEntity(existing, cancellationToken: cancellationToken);

        return Result<UsuarioDto>.Success(
            await RecordUpdate(carnet, previous, existing, cancellationToken)
        );
    }

    private async Task ApplyChanges(
        UsuarioDto dto,
        UsuarioEntity existing,
        bool isAdmin,
        CancellationToken cancellationToken
    )
    {
        if (isAdmin)
            await ResolveCarrera(dto, cancellationToken);
        ProtectProfileDocuments(dto, existing);
        _mapper.Update(dto, existing);

        if (isAdmin && (dto.IdCarrera ?? 0) > 0)
            existing.IdCarrera = dto.IdCarrera!.Value;

        if (!string.IsNullOrWhiteSpace(dto.Contrasena))
            existing.Contrasena = BCryptLib.HashPassword(dto.Contrasena, workFactor: 12);
    }

    private async Task<UsuarioDto> RecordUpdate(
        string carnet,
        UsuarioDto previous,
        UsuarioEntity existing,
        CancellationToken cancellationToken
    )
    {
        var resultDto = await ToDtoWithCarrera(existing, cancellationToken);
        var auditDetail = AuditChangeDetail.Build(previous, resultDto);
        ProfileDocuments.Clear(resultDto);

        _ = await _cacheRepository.Remove(CacheKeys.Usuario(carnet), cancellationToken);
        await Audit!.Log(
            AuditAccion.Editar,
            typeof(UsuarioEntity).Name,
            carnet,
            auditDetail
        );

        return resultDto;
    }

    private async Task<UsuarioDto> ToDtoWithCarrera(
        UsuarioEntity user,
        CancellationToken cancellationToken
    )
    {
        var dto = _mapper.ToDto(user);
        dto.CarreraNombre = await _queries.GetCarreraName(user.IdCarrera, cancellationToken);
        return dto;
    }

    public async Task<Result<UsuarioDto>> UpdateProfile(
        string carnet,
        UsuarioDto dto,
        CancellationToken cancellationToken = default
    )
    {
        var result = await Update(
            carnet,
            dto,
            carnet,
            cancellationToken: cancellationToken
        );

        return result.IsSuccess
            ? await GetProfile(carnet, cancellationToken)
            : result;
    }

    public async Task<Result<UsuarioDto>> GetVisible(
        string carnet,
        string carnetActual,
        bool puedeGestionar,
        CancellationToken cancellationToken = default
    ) =>
        puedeGestionar || carnetActual == carnet
            ? await Get(carnet, cancellationToken)
            : Result<UsuarioDto>.Forbidden();

    public async Task<Result<UsuarioDto>> Get(
        string carnet,
        CancellationToken cancellationToken = default
    )
    {
        var cacheKey = CacheKeys.Usuario(carnet);
        var cacheResult = await _cacheRepository.Get<UsuarioDto>(cacheKey, cancellationToken);

        if (cacheResult.IsSuccess)
            return Result<UsuarioDto>.Success(cacheResult.Value);

        var user = await _queries.GetByCarnet(carnet, cancellationToken);

        if (user == null)
            return Result<UsuarioDto>.NotFound();

        var dto = await ToDtoWithCarrera(user, cancellationToken);
        ProfileDocuments.Clear(dto);

        _ = await _cacheRepository.Set(cacheKey, dto, UsuarioCacheTtl, cancellationToken);

        return Result<UsuarioDto>.Success(dto);
    }

    public async Task<Result<UsuarioDto>> GetProfile(
        string carnet,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _queries.GetByCarnet(carnet, cancellationToken);

        if (user == null)
            return Result<UsuarioDto>.NotFound();

        var dto = await ToDtoWithCarrera(user, cancellationToken);
        SetProfileDocuments(dto, user);
        return Result<UsuarioDto>.Success(dto);
    }

    public async Task<Result<object>> Delete(
        string carnet,
        CancellationToken cancellationToken = default
    )
    {
        var previous = await Repository.GetTrackedByCarnet(carnet, cancellationToken);
        var previousDto = previous == null ? null : _mapper.ToDto(previous);
        var deleteResult = await Repository.Delete(carnet, cancellationToken);

        if (deleteResult.IsSuccess)
        {
            _ = await _cacheRepository.Remove(CacheKeys.Usuario(carnet), CancellationToken.None);
            await Audit!.Log(
                AuditAccion.Eliminar,
                typeof(UsuarioEntity).Name,
                carnet,
                AuditChangeDetail.BuildDeleted(previousDto)
            );
        }

        return deleteResult;
    }

    private async Task ResolveCarrera(
        UsuarioDto dto,
        CancellationToken cancellationToken = default
    )
    {
        if ((dto.IdCarrera ?? 0) > 0)
            return;

        if (string.IsNullOrWhiteSpace(dto.CarreraNombre))
            return;

        dto.IdCarrera = await _queries.FindCarreraIdByName(
            dto.CarreraNombre,
            cancellationToken
        );
    }

    private static void PreserveTraceableFields(UsuarioDto dto, UsuarioEntity existing)
    {
        dto.Carnet = existing.Carnet;
        dto.Nombre = existing.Nombre;
        dto.ApellidoPaterno = existing.ApellidoPaterno;
        dto.ApellidoMaterno = existing.ApellidoMaterno;
        dto.Email = existing.Email;
        dto.IdCarrera = existing.IdCarrera;
        dto.Rol = existing.Rol.ToString().ToLowerInvariant();
    }

    private void ProtectProfileDocuments(UsuarioDto dto, UsuarioEntity? existing)
    {
        dto.ImagenPerfil = ProtectOrKeep(dto.ImagenPerfil, existing?.ImagenPerfil);
        dto.ImagenFrenteCarnet = ProtectOrKeep(dto.ImagenFrenteCarnet, existing?.ImagenFrenteCarnet);
        dto.ImagenAtrasCarnet = ProtectOrKeep(dto.ImagenAtrasCarnet, existing?.ImagenAtrasCarnet);
        dto.ImagenFirma = ProtectOrKeep(dto.ImagenFirma, existing?.ImagenFirma);
    }

    private byte[]? ProtectOrKeep(byte[]? value, byte[]? current) =>
        value == null ? current : _sensitiveData.Protect(value);

    private void SetProfileDocuments(UsuarioDto dto, UsuarioEntity user)
    {
        dto.ImagenPerfil = _sensitiveData.Unprotect(user.ImagenPerfil);
        dto.ImagenFrenteCarnet = _sensitiveData.Unprotect(user.ImagenFrenteCarnet);
        dto.ImagenAtrasCarnet = _sensitiveData.Unprotect(user.ImagenAtrasCarnet);
        dto.ImagenFirma = _sensitiveData.Unprotect(user.ImagenFirma);
    }
}
