using Ardalis.Result;
using Microsoft.Extensions.Options;
using BCryptLib = BCrypt.Net.BCrypt;
using UsuarioEntity = UCB_Hold.Server.Modules.Usuarios.Domain.Usuario;
using UCB_Hold.Server.Modules.Usuarios.Infrastructure;

namespace UCB_Hold.Server.Modules.Usuarios.Application;

public sealed class SesionService
{
    private const int MaxRefreshTokenLength = 256;
    private static readonly string UnknownUserPasswordHash = BCryptLib.HashPassword(
        AuthTokenGenerator.Create(),
        workFactor: 12
    );
    private readonly UsuarioAuthRepository _authRepository;
    private readonly UsuarioMapper _mapper;
    private readonly JwtService _jwtService;
    private readonly JwtSettings _jwtSettings;

    public SesionService(
        UsuarioAuthRepository authRepository,
        UsuarioMapper mapper,
        JwtService jwtService,
        IOptions<JwtSettings> jwtSettings
    )
    {
        _authRepository = authRepository;
        _mapper = mapper;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings.Value;
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

        var storedHash = string.IsNullOrWhiteSpace(user?.Contrasena)
            ? UnknownUserPasswordHash
            : user.Contrasena;
        var passwordValid = BCryptLib.Verify(password, storedHash);

        if (user == null || !passwordValid)
            return Result<LoginDto>.Unauthorized("Credenciales inválidas");

        if (!user.EmailVerificado)
            return Result<LoginDto>.Unauthorized("Debes verificar tu correo antes de iniciar sesión");

        return Result<LoginDto>.Success(await Issue(user, carreraNombre, cancellationToken));
    }

    public async Task<LoginDto> Issue(
        UsuarioEntity user,
        string? carreraNombre,
        CancellationToken cancellationToken = default
    )
    {
        var dto = ToSessionUser(user, carreraNombre);
        var refreshToken = JwtService.GenerateRefreshToken();

        await _authRepository.UpdateRefreshToken(
            user.Carnet!,
            JwtService.HashRefreshToken(refreshToken),
            RefreshTokenExpiry(),
            cancellationToken
        );

        return BuildSession(dto, refreshToken);
    }

    public async Task<Result<LoginDto>> Refresh(
        string refreshToken,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<LoginDto>.Unauthorized("Refresh token requerido");

        if (refreshToken.Length > MaxRefreshTokenLength)
            return Result<LoginDto>.Unauthorized("Refresh token inválido");

        var refreshTokenHash = JwtService.HashRefreshToken(refreshToken);
        var (user, carreraNombre) = await _authRepository.GetByRefreshTokenWithCarrera(
            refreshTokenHash,
            cancellationToken
        );

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Result<LoginDto>.Unauthorized("Refresh token inválido o expirado");

        var dto = ToSessionUser(user, carreraNombre);
        var newRefreshToken = JwtService.GenerateRefreshToken();

        var rotated = await _authRepository.RotateRefreshToken(
            user.Carnet!,
            refreshTokenHash,
            JwtService.HashRefreshToken(newRefreshToken),
            RefreshTokenExpiry(),
            cancellationToken
        );

        if (!rotated)
            return Result<LoginDto>.Unauthorized("Refresh token inválido o ya utilizado");

        return Result<LoginDto>.Success(BuildSession(dto, newRefreshToken));
    }

    public async Task Logout(
        string refreshToken,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > MaxRefreshTokenLength)
            return;

        await _authRepository.RevokeRefreshToken(
            JwtService.HashRefreshToken(refreshToken),
            cancellationToken
        );
    }

    private UsuarioDto ToSessionUser(UsuarioEntity user, string? carreraNombre)
    {
        var dto = _mapper.ToDto(user);
        dto.CarreraNombre = carreraNombre;
        ProfileDocuments.Clear(dto);
        return dto;
    }

    private LoginDto BuildSession(UsuarioDto dto, string refreshToken) =>
        new()
        {
            AccessToken = _jwtService.GenerateAccessToken(dto),
            RefreshToken = refreshToken,
            Usuario = dto,
        };

    private DateTime RefreshTokenExpiry() =>
        DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
}
