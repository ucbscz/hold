using Ardalis.Result;
using System.Text.Json;
using FluentValidation;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Application.Features.Cache;
using IMT_Reservas.Server.Application.Features.Jwt;
using IMT_Reservas.Server.Application.Features.Notificacion;
using IMT_Reservas.Server.Application.Security;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.Extensions.Options;
using BCryptLib = BCrypt.Net.BCrypt;
using UsuarioEntity = IMT_Reservas.Server.Core.Entities.Usuario;

namespace IMT_Reservas.Server.Application.Features.Usuario;

public class UsuarioService : Service<UsuarioEntity, UsuarioRepository, UsuarioDto>
{
    public const string TermsVersion = "2026-09-02";
    private static readonly TimeSpan UsuarioCacheTtl = TimeSpan.FromMinutes(30);
    private readonly UsuarioMapper _mapper;
    private readonly JwtService _jwtService;
    private readonly JwtSettings _jwtSettings;
    private readonly CacheRepository _cacheRepository;
    private readonly NotificacionService _notifications;
    private readonly UsuarioAuthRepository _authRepository;
    private readonly UsuarioReadRepository _queries;
    private readonly SensitiveDataProtector _sensitiveData;
    private readonly VerificacionCorreoService _emailVerification;
    private readonly CodigoGoogleService _googleCodes;

    public UsuarioService(
        UsuarioRepository repository,
        UsuarioMapper mapper,
        IValidator<UsuarioDto> validator,
        JwtService jwtService,
        IOptions<JwtSettings> jwtSettings,
        CacheRepository cacheRepository,
        AuditLogService audit,
        NotificacionService notifications,
        UsuarioAuthRepository authRepository,
        UsuarioReadRepository queries,
        SensitiveDataProtector sensitiveData,
        VerificacionCorreoService emailVerification,
        CodigoGoogleService googleCodes
    )
        : base(repository, validator, mapper, audit)
    {
        _mapper = mapper;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings.Value;
        _cacheRepository = cacheRepository;
        _notifications = notifications;
        _authRepository = authRepository;
        _queries = queries;
        _sensitiveData = sensitiveData;
        _emailVerification = emailVerification;
        _googleCodes = googleCodes;
    }

    public async Task<Result<object>> SetBlocked(
        string carnet,
        bool isBlocked,
        string? blockReason,
        bool isAdmin,
        string? actorCarnet = null,
        CancellationToken cancellationToken = default,
        bool isLabAdmin = false
    )
    {
        if (!isAdmin)
            return Result<object>.Forbidden();

        var user = await Repository.GetTrackedByCarnet(carnet, cancellationToken);

        if (user == null)
            return Result<object>.NotFound();

        if (isLabAdmin && user.Rol is Core.Entities.TipoUsuario.Administrador or Core.Entities.TipoUsuario.Administrador_Laboratorio)
            return Result<object>.Forbidden();

        user.Bloqueado = isBlocked;
        user.MotivoBloqueo = isBlocked ? blockReason : null;
        await Repository.UpdateEntity(
            user,
            saveChanges: false,
            cancellationToken: cancellationToken
        );

        await Audit!.Log(
            isBlocked ? AuditAccion.Bloquear : AuditAccion.Desbloquear,
            typeof(UsuarioEntity).Name,
            carnet,
            blockReason,
            saveChanges: false
        );

        var actorName = string.IsNullOrWhiteSpace(actorCarnet)
            ? null
            : await _queries.GetDisplayName(actorCarnet, cancellationToken);
        var notificationType = isBlocked
            ? TipoNotificacion.UsuarioBloqueado
            : TipoNotificacion.UsuarioDesbloqueado;
        var notificationTitle = isBlocked
            ? "Cuenta bloqueada para reservas"
            : "Cuenta desbloqueada para reservas";
        var notificationReason = isBlocked
            ? blockReason ?? "Bloqueo administrativo para nuevas reservas."
            : "Desbloqueo administrativo de la cuenta.";
        var notificationContent = isBlocked
            ? string.IsNullOrWhiteSpace(blockReason)
                ? "Tu cuenta fue bloqueada para nuevas reservas. Contacta con un administrador para revisar tu caso."
                : $"{blockReason}. Contacta con un administrador para revisar tu caso."
            : "Tu cuenta fue desbloqueada. Ya puedes realizar nuevas reservas.";

        await _notifications.Create(
            carnet,
            notificationType,
            notificationTitle,
            notificationContent,
            JsonSerializer.Serialize(new
            {
                emisor = actorName ?? "Sistema",
                motivo = notificationReason,
                fecha = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
            }),
            saveChanges: false
        );

        await Repository.SaveChanges(cancellationToken);
        _ = _cacheRepository.Remove(CacheKeys.Usuario(carnet));

        return Result<object>.Success(null!);
    }

    public Task<Result<List<UsuarioDto>>> GetAll(
        int page,
        int pageSize,
        CancellationToken cancellationToken
    ) => Repository.GetPage(page, pageSize, cancellationToken);

    public override async Task<Result<UsuarioDto>> Create(UsuarioDto dto) =>
        await Create(dto, isAdmin: false);

    public async Task<Result<UsuarioDto>> Create(UsuarioDto dto, bool isAdmin, bool isLabAdmin = false)
    {
        dto.Carnet = dto.Carnet?.Trim().ToUpperInvariant();
        dto.Rol = dto.Rol?.Trim().ToLowerInvariant();
        dto.Email = dto.Email?.Trim().ToLowerInvariant();
        var googleRegistration = string.IsNullOrWhiteSpace(dto.CodigoGoogle)
            ? null
            : await _googleCodes.Get(dto.CodigoGoogle, CancellationToken.None);
        if (googleRegistration != null && googleRegistration.Tipo != "registro")
            googleRegistration = null;

        if (!string.IsNullOrWhiteSpace(dto.CodigoGoogle) && googleRegistration == null)
            return Result<UsuarioDto>.Unauthorized("El registro con Google expiró o ya fue utilizado");

        if (googleRegistration != null)
        {
            dto.Email = googleRegistration.Email;
            dto.Contrasena = $"Gg1!{AuthTokenGenerator.Create()}";
            dto.EmailVerificado = true;
        }

        if (string.IsNullOrWhiteSpace(dto.Contrasena))
            return Result<UsuarioDto>.Error("Contraseña requerida");

        if (!isAdmin)
            dto.Rol = null;

        if (!isAdmin && dto.AceptaTerminos != true)
            return Result<UsuarioDto>.Error(
                "Debes aceptar los términos y condiciones para crear tu cuenta"
            );

        if (isLabAdmin && dto.Rol is "administrador" or "administrador_laboratorio")
            return Result<UsuarioDto>.Forbidden();
        dto.Bloqueado = false;
        dto.MotivoBloqueo = null;
        dto.EmailVerificado = isAdmin || googleRegistration != null;

        await ResolveCarrera(dto);

        var validation = await Validator.ValidateAsync(dto);

        if (!validation.IsValid)
            return validation.ToResult<UsuarioDto>();

        var deletedUser = await Repository.GetDeletedByCarnet(dto.Carnet!);

        if (deletedUser == null && await _queries.ExistsByCarnet(dto.Carnet!))
            return Result<UsuarioDto>.Error("Carnet ya existe");

        if (await _queries.ExistsByEmail(dto.Email!, deletedUser?.Carnet))
        {
            var existingEmail = await _authRepository.GetTrackedByEmail(dto.Email!);
            return Result<UsuarioDto>.Error(
                string.IsNullOrWhiteSpace(existingEmail?.GoogleId)
                    ? "Email ya existe"
                    : "El correo ya está registrado. Inicia sesión con Google"
            );
        }

        if (
            !string.IsNullOrWhiteSpace(dto.Telefono)
            && await _queries.ExistsByTelefono(dto.Telefono, deletedUser?.Carnet)
        )
            return Result<UsuarioDto>.Error("Teléfono ya registrado");

        dto.ImagenPerfil = _sensitiveData.Protect(dto.ImagenPerfil);
        dto.ImagenFrenteCarnet = _sensitiveData.Protect(dto.ImagenFrenteCarnet);
        dto.ImagenAtrasCarnet = _sensitiveData.Protect(dto.ImagenAtrasCarnet);
        dto.ImagenFirma = _sensitiveData.Protect(dto.ImagenFirma);
        var entity = deletedUser ?? MapToEntity(dto);

        if (deletedUser != null)
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

        entity.EmailVerificado = isAdmin || googleRegistration != null;
        entity.GoogleId = googleRegistration?.GoogleId;
        entity.TokenVerificacionHash = null;
        entity.TokenVerificacionExpira = null;

        entity.Contrasena = BCryptLib.HashPassword(dto.Contrasena, workFactor: 12);
        Result<UsuarioDto> result;

        if (deletedUser == null)
        {
            result = await CreateEntity(entity);
        }
        else
        {
            await Repository.UpdateEntity(entity);
            result = Result<UsuarioDto>.Success(_mapper.ToDto(entity));
        }

        if (result.IsSuccess && result.Value != null)
        {
            if (googleRegistration != null)
            {
                if (!await _googleCodes.Consume(dto.CodigoGoogle!, CancellationToken.None))
                    return Result<UsuarioDto>.Conflict("El registro con Google ya fue utilizado");
            }
            else if (!isAdmin)
            {
                result.Value.EmailVerificacionEnviada = await _emailVerification.Issue(
                    entity,
                    CancellationToken.None
                );
            }
            result.Value.CarreraNombre = await _queries.GetCarreraName(entity.IdCarrera);
            ClearProfileDocuments(result.Value);
            await Audit!.Log(
                AuditAccion.Crear,
                typeof(UsuarioEntity).Name,
                entity.Carnet,
                JsonSerializer.Serialize(new
                {
                    cuentaRecreada = deletedUser != null,
                    aceptoTerminos = dto.AceptaTerminos == true,
                    versionTerminos = dto.AceptaTerminos == true ? TermsVersion : null,
                    fechaAceptacion = dto.AceptaTerminos == true
                        ? (DateTime?)DateTime.UtcNow
                        : null,
                })
            );
        }

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

        if (!isAdmin)
            PreserveTraceableFields(dto, existing);

        dto.Rol = dto.Rol?.Trim().ToLowerInvariant();
        dto.ImagenPerfil = dto.ImagenPerfil == null
            ? existing.ImagenPerfil
            : _sensitiveData.Protect(dto.ImagenPerfil);
        dto.ImagenFrenteCarnet = dto.ImagenFrenteCarnet == null
            ? existing.ImagenFrenteCarnet
            : _sensitiveData.Protect(dto.ImagenFrenteCarnet);
        dto.ImagenAtrasCarnet = dto.ImagenAtrasCarnet == null
            ? existing.ImagenAtrasCarnet
            : _sensitiveData.Protect(dto.ImagenAtrasCarnet);
        dto.ImagenFirma = dto.ImagenFirma == null
            ? existing.ImagenFirma
            : _sensitiveData.Protect(dto.ImagenFirma);

        var validation = await Validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
            return validation.ToResult<UsuarioDto>();

        if (isLabAdmin && (existing.Rol is Core.Entities.TipoUsuario.Administrador or Core.Entities.TipoUsuario.Administrador_Laboratorio
            || dto.Rol is "administrador" or "administrador_laboratorio"))
            return Result<UsuarioDto>.Forbidden();
        dto.Bloqueado = existing.Bloqueado;
        dto.MotivoBloqueo = existing.MotivoBloqueo;

        if (
            !string.IsNullOrWhiteSpace(dto.Telefono)
            && await _queries.ExistsByTelefono(dto.Telefono, carnet, cancellationToken)
        )
            return Result<UsuarioDto>.Error("Teléfono ya registrado");

        if (isAdmin)
            await ResolveCarrera(dto, cancellationToken);
        _mapper.Update(dto, existing);

        if (isAdmin && (dto.IdCarrera ?? 0) > 0)
            existing.IdCarrera = dto.IdCarrera!.Value;

        if (!string.IsNullOrWhiteSpace(dto.Contrasena))
            existing.Contrasena = BCryptLib.HashPassword(dto.Contrasena, workFactor: 12);

        await Repository.UpdateEntity(existing, cancellationToken: cancellationToken);

        var resultDto = _mapper.ToDto(existing);
        resultDto.CarreraNombre = await _queries.GetCarreraName(
            existing.IdCarrera,
            cancellationToken
        );
        ClearProfileDocuments(resultDto);

        _ = await _cacheRepository.Remove(CacheKeys.Usuario(carnet));
        await Audit!.Log(AuditAccion.Editar, typeof(UsuarioEntity).Name, carnet);

        return Result<UsuarioDto>.Success(resultDto);
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

    public async Task<Result<UsuarioDto>> Get(string carnet)
    {
        var cacheKey = CacheKeys.Usuario(carnet);
        var cacheResult = await _cacheRepository.Get<UsuarioDto>(cacheKey);

        if (cacheResult.IsSuccess)
            return Result<UsuarioDto>.Success(cacheResult.Value);

        var user = await _queries.GetByCarnet(carnet);

        if (user == null)
            return Result<UsuarioDto>.NotFound();

        var dto = _mapper.ToDto(user);
        dto.CarreraNombre = await _queries.GetCarreraName(user.IdCarrera);
        ClearProfileDocuments(dto);

        _ = await _cacheRepository.Set(cacheKey, dto, UsuarioCacheTtl);

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

        var dto = _mapper.ToDto(user);
        dto.CarreraNombre = await _queries.GetCarreraName(
            user.IdCarrera,
            cancellationToken
        );
        SetProfileDocuments(dto, user);
        return Result<UsuarioDto>.Success(dto);
    }

    public async Task<Result<LoginDto>> Login(
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        email = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Result<LoginDto>.Unauthorized("Credenciales requeridas");

        if (email.Length > 255 || password.Length > 72)
            return Result<LoginDto>.Unauthorized("Credenciales inválidas");

        var (user, carreraNombre) = await _authRepository.GetByEmailWithCarrera(
            email,
            cancellationToken
        );

        if (user == null)
            return Result<LoginDto>.Unauthorized("Credenciales inválidas");

        var passwordValid =
            !string.IsNullOrWhiteSpace(user.Contrasena)
            && BCryptLib.Verify(password, user.Contrasena);

        if (!passwordValid)
            return Result<LoginDto>.Unauthorized("Credenciales inválidas");

        if (!user.EmailVerificado)
            return Result<LoginDto>.Unauthorized("Debes verificar tu correo antes de iniciar sesión");

        var dto = _mapper.ToDto(user);
        dto.CarreraNombre = carreraNombre;
        ClearProfileDocuments(dto);

        var accessToken = _jwtService.GenerateAccessToken(dto);
        var refreshToken = JwtService.GenerateRefreshToken();

        await _authRepository.UpdateRefreshToken(
            user.Carnet!,
            JwtService.HashRefreshToken(refreshToken),
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            cancellationToken
        );

        return Result<LoginDto>.Success(
            new LoginDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Usuario = dto,
            }
        );
    }

    public async Task<Result<LoginDto>> Refresh(
        string refreshToken,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<LoginDto>.Unauthorized("Refresh token requerido");

        if (refreshToken.Length > 256)
            return Result<LoginDto>.Unauthorized("Refresh token inválido");

        var refreshTokenHash = JwtService.HashRefreshToken(refreshToken);
        var (user, carreraNombre) = await _authRepository.GetByRefreshTokenWithCarrera(
            refreshTokenHash,
            cancellationToken
        );

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Result<LoginDto>.Unauthorized("Refresh token inválido o expirado");

        var dto = _mapper.ToDto(user);
        dto.CarreraNombre = carreraNombre;
        ClearProfileDocuments(dto);

        var newAccessToken = _jwtService.GenerateAccessToken(dto);
        var newRefreshToken = JwtService.GenerateRefreshToken();

        var rotated = await _authRepository.RotateRefreshToken(
            user.Carnet!,
            refreshTokenHash,
            JwtService.HashRefreshToken(newRefreshToken),
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            cancellationToken
        );

        if (!rotated)
            return Result<LoginDto>.Unauthorized("Refresh token inválido o ya utilizado");

        return Result<LoginDto>.Success(
            new LoginDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Usuario = dto,
            }
        );
    }

    public async Task<Result<object>> Delete(string carnet)
    {
        var deleteResult = await Repository.Delete(carnet);

        if (deleteResult.IsSuccess)
        {
            _ = await _cacheRepository.Remove(CacheKeys.Usuario(carnet));
            await Audit!.Log(AuditAccion.Eliminar, typeof(UsuarioEntity).Name, carnet);
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

    private void SetProfileDocuments(UsuarioDto dto, UsuarioEntity user)
    {
        dto.ImagenPerfil = _sensitiveData.Unprotect(user.ImagenPerfil);
        dto.ImagenFrenteCarnet = _sensitiveData.Unprotect(user.ImagenFrenteCarnet);
        dto.ImagenAtrasCarnet = _sensitiveData.Unprotect(user.ImagenAtrasCarnet);
        dto.ImagenFirma = _sensitiveData.Unprotect(user.ImagenFirma);
    }

    private static void ClearProfileDocuments(UsuarioDto dto)
    {
        dto.ImagenPerfil = null;
        dto.ImagenFrenteCarnet = null;
        dto.ImagenAtrasCarnet = null;
        dto.ImagenFirma = null;
    }
}
