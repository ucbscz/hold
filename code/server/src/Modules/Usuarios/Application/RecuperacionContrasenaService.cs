using Ardalis.Result;
using BCryptLib = BCrypt.Net.BCrypt;
using IMT_Reservas.Server.Modules.Usuarios.Domain;
using IMT_Reservas.Server.Modules.Usuarios.Infrastructure;

namespace IMT_Reservas.Server.Modules.Usuarios.Application;

public sealed class RecuperacionContrasenaService
{
    private const string ResetCodeType = "recuperar_contrasena";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);
    private readonly CodigoAutenticacionRepository _codes;
    private readonly UsuarioAuthRepository _users;
    private readonly EmailDeliveryService _email;

    public RecuperacionContrasenaService(
        CodigoAutenticacionRepository codes,
        UsuarioAuthRepository users,
        EmailDeliveryService email
    )
    {
        _codes = codes;
        _users = users;
        _email = email;
    }

    public async Task<Result<object>> Request(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 255)
            return Result<object>.Invalid(
                new ValidationError("Email", "Ingresa un correo institucional válido")
            );

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _users.GetTrackedByEmail(normalizedEmail, cancellationToken);
        if (user == null)
            return Result<object>.NotFound("No encontramos una cuenta con ese correo");
        if (!_email.IsEnabled)
            return Result<object>.Error(
                "El envío de correo no está configurado. Contacta con un administrador"
            );

        var token = AuthTokenGenerator.CreateNumericCode();
        await _codes.Create(
            new CodigoAutenticacion
            {
                Hash = AuthTokenGenerator.Hash($"{normalizedEmail}:{token}"),
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
        if (!await _email.SendPasswordReset(user.Email, token, TokenLifetime, cancellationToken))
            return Result<object>.Error("No se pudo enviar el correo de recuperación");

        return Result<object>.Success(null!);
    }

    public async Task<Result<object>> Reset(
        string email,
        string token,
        string password,
        CancellationToken cancellationToken
    )
    {
        if (
            string.IsNullOrWhiteSpace(email)
            || email.Length > 255
            || string.IsNullOrWhiteSpace(token)
            || token.Length != 6
        )
            return InvalidToken();

        var passwordResult = ValidatePassword(password);
        if (passwordResult != null)
            return passwordResult;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var hash = AuthTokenGenerator.Hash($"{normalizedEmail}:{token}");
        var code = await _codes.GetActive(hash, cancellationToken);
        if (code == null || code.Tipo != ResetCodeType)
            return InvalidToken();

        if (!await _codes.Consume(hash, cancellationToken))
            return InvalidToken();

        var user = await _users.GetTrackedByEmail(normalizedEmail, cancellationToken);
        if (user == null)
            return InvalidToken();

        user.Contrasena = BCryptLib.HashPassword(password, workFactor: 12);
        user.EmailVerificado = true;
        user.TokenVerificacionHash = null;
        user.TokenVerificacionExpira = null;
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
