using Ardalis.Result;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using BCryptLib = BCrypt.Net.BCrypt;

namespace IMT_Reservas.Server.Application.Features.Usuario;

public sealed class RecuperacionContrasenaService
{
    private const string ResetCodeType = "recuperar_contrasena";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);
    private readonly CodigoAutenticacionRepository _codes;
    private readonly UsuarioAuthRepository _users;
    private readonly EmailDeliveryService _email;
    private readonly string _frontendUrl;

    public RecuperacionContrasenaService(
        CodigoAutenticacionRepository codes,
        UsuarioAuthRepository users,
        EmailDeliveryService email,
        IConfiguration configuration
    )
    {
        _codes = codes;
        _users = users;
        _email = email;
        _frontendUrl = configuration["Authentication:FrontendUrl"]?.TrimEnd('/')
            ?? "http://localhost:4200";
    }

    public async Task<Result<object>> Request(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 255)
            return Result<object>.Success(null!);

        var user = await _users.GetTrackedByEmail(email.Trim().ToLowerInvariant(), cancellationToken);
        if (user == null || !user.EmailVerificado || !string.IsNullOrWhiteSpace(user.GoogleId))
            return Result<object>.Success(null!);

        var token = AuthTokenGenerator.Create();
        await _codes.Create(
            new CodigoAutenticacion
            {
                Hash = AuthTokenGenerator.Hash(token),
                Tipo = ResetCodeType,
                Email = user.Email,
                GoogleId = string.Empty,
                Nombre = string.Empty,
                ApellidoPaterno = string.Empty,
                ApellidoMaterno = string.Empty,
                Expira = DateTime.UtcNow.Add(TokenLifetime),
            },
            cancellationToken
        );
        var url = $"{_frontendUrl}/recuperar?token={Uri.EscapeDataString(token)}";
        await _email.SendPasswordReset(user.Email, url, cancellationToken);
        return Result<object>.Success(null!);
    }

    public async Task<Result<object>> Reset(
        string token,
        string password,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256)
            return InvalidToken();

        var passwordResult = ValidatePassword(password);
        if (passwordResult != null)
            return passwordResult;

        var hash = AuthTokenGenerator.Hash(token);
        var code = await _codes.GetActive(hash, cancellationToken);
        if (code == null || code.Tipo != ResetCodeType)
            return InvalidToken();

        if (!await _codes.Consume(hash, cancellationToken))
            return InvalidToken();

        var user = await _users.GetTrackedByEmail(code.Email, cancellationToken);
        if (user == null || !user.EmailVerificado || !string.IsNullOrWhiteSpace(user.GoogleId))
            return InvalidToken();

        user.Contrasena = BCryptLib.HashPassword(password, workFactor: 12);
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _users.UpdateCredentials(user, cancellationToken);
        return Result<object>.Success(null!);
    }

    private static Result<object> InvalidToken() =>
        Result<object>.Invalid(new ValidationError("Token", "El enlace ya fue utilizado o expiró"));

    private static Result<object>? ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return Result<object>.Invalid(new ValidationError("Contrasena", "La contraseña debe tener al menos 8 caracteres"));
        if (password.Length > 72)
            return Result<object>.Invalid(new ValidationError("Contrasena", "La contraseña no puede superar 72 caracteres"));
        if (!password.Any(char.IsUpper))
            return Result<object>.Invalid(new ValidationError("Contrasena", "La contraseña debe tener al menos una mayúscula"));
        if (!password.Any(char.IsDigit))
            return Result<object>.Invalid(new ValidationError("Contrasena", "La contraseña debe tener al menos un número"));
        if (!password.Any(character => !char.IsLetterOrDigit(character)))
            return Result<object>.Invalid(new ValidationError("Contrasena", "La contraseña debe tener al menos un carácter especial"));
        return null;
    }
}
