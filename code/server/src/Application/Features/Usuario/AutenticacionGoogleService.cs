using System.Security.Claims;
using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Jwt;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.Extensions.Options;

namespace IMT_Reservas.Server.Application.Features.Usuario;

public sealed class AutenticacionGoogleService
{
    private readonly CodigoGoogleService _codes;
    private readonly UsuarioAuthRepository _users;
    private readonly UsuarioMapper _mapper;
    private readonly JwtService _jwt;
    private readonly JwtSettings _jwtSettings;

    public AutenticacionGoogleService(
        CodigoGoogleService codes,
        UsuarioAuthRepository users,
        UsuarioMapper mapper,
        JwtService jwt,
        IOptions<JwtSettings> jwtSettings
    )
    {
        _codes = codes;
        _users = users;
        _mapper = mapper;
        _jwt = jwt;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<string>> Begin(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken
    )
    {
        var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email)?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(googleId) || !IsInstitutionalEmail(email))
            return Result<string>.Unauthorized("Debes usar una cuenta institucional @ucb.edu.bo");

        var (user, _) = await _users.GetByEmailWithCarrera(email!, cancellationToken);
        var type = user == null ? "registro" : "login";
        if (user != null)
        {
            if (!string.IsNullOrWhiteSpace(user.GoogleId) && user.GoogleId != googleId)
                return Result<string>.Unauthorized("El correo ya está vinculado con otra cuenta de Google");
            await _users.LinkGoogle(user.Carnet, googleId, cancellationToken);
        }

        var fullName = principal.FindFirstValue(ClaimTypes.Name)?.Trim() ?? string.Empty;
        var givenName = principal.FindFirstValue(ClaimTypes.GivenName)?.Trim();
        var surname = principal.FindFirstValue(ClaimTypes.Surname)?.Trim() ?? string.Empty;
        var name = string.IsNullOrWhiteSpace(givenName)
            ? fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty
            : givenName;
        var surnames = surname.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var code = await _codes.Create(
            type,
            email!,
            googleId,
            name,
            surnames.FirstOrDefault() ?? string.Empty,
            surnames.Skip(1).FirstOrDefault() ?? string.Empty,
            cancellationToken
        );
        return Result<string>.Success(code);
    }

    public async Task<Result<GoogleAuthDto>> Exchange(
        string token,
        CancellationToken cancellationToken
    )
    {
        var code = await _codes.Get(token, cancellationToken);
        if (code == null)
            return Result<GoogleAuthDto>.Unauthorized("El acceso con Google expiró o ya fue utilizado");

        if (code.Tipo == "registro")
            return Result<GoogleAuthDto>.Success(new GoogleAuthDto
            {
                RequiereRegistro = true,
                CodigoRegistro = token,
                Email = code.Email,
                Nombre = code.Nombre,
                ApellidoPaterno = code.ApellidoPaterno,
                ApellidoMaterno = code.ApellidoMaterno,
            });

        if (!await _codes.Consume(token, cancellationToken))
            return Result<GoogleAuthDto>.Unauthorized("El acceso con Google ya fue utilizado");

        var (user, careerName) = await _users.GetByEmailWithCarrera(code.Email, cancellationToken);
        if (user == null || user.GoogleId != code.GoogleId)
            return Result<GoogleAuthDto>.Unauthorized("No se pudo vincular la cuenta de Google");

        var dto = _mapper.ToDto(user);
        dto.CarreraNombre = careerName;
        dto.ImagenFrenteCarnet = null;
        dto.ImagenAtrasCarnet = null;
        dto.ImagenFirma = null;
        var accessToken = _jwt.GenerateAccessToken(dto);
        var refreshToken = JwtService.GenerateRefreshToken();
        await _users.UpdateRefreshToken(
            user.Carnet,
            JwtService.HashRefreshToken(refreshToken),
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            cancellationToken
        );

        return Result<GoogleAuthDto>.Success(new GoogleAuthDto
        {
            Sesion = new LoginDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Usuario = dto,
            },
        });
    }

    private static bool IsInstitutionalEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email)
        && email.EndsWith("@ucb.edu.bo", StringComparison.OrdinalIgnoreCase);
}
