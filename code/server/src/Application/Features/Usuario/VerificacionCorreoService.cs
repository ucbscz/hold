using Ardalis.Result;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.Extensions.Options;
using UsuarioEntity = IMT_Reservas.Server.Core.Entities.Usuario;

namespace IMT_Reservas.Server.Application.Features.Usuario;

public sealed class VerificacionCorreoService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);
    private readonly UsuarioAuthRepository _repository;
    private readonly EmailDeliveryService _email;
    private readonly string _frontendUrl;

    public VerificacionCorreoService(
        UsuarioAuthRepository repository,
        EmailDeliveryService email,
        IConfiguration configuration
    )
    {
        _repository = repository;
        _email = email;
        _frontendUrl = configuration["Authentication:FrontendUrl"]?.TrimEnd('/')
            ?? "http://localhost:4200";
    }

    public async Task<bool> Issue(UsuarioEntity user, CancellationToken cancellationToken)
    {
        var token = AuthTokenGenerator.Create();
        user.TokenVerificacionHash = AuthTokenGenerator.Hash(token);
        user.TokenVerificacionExpira = DateTime.UtcNow.Add(TokenLifetime);
        await _repository.SaveVerification(user, cancellationToken);
        var url = $"{_frontendUrl}/verificar?token={Uri.EscapeDataString(token)}";
        return await _email.SendVerification(user.Email, url, cancellationToken);
    }

    public async Task<Result<object>> Confirm(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256)
            return Result<object>.Invalid(new ValidationError("Token", "Enlace de verificación inválido"));

        var confirmed = await _repository.ConfirmEmail(
            AuthTokenGenerator.Hash(token),
            cancellationToken
        );
        return confirmed
            ? Result<object>.Success(null!)
            : Result<object>.Invalid(new ValidationError("Token", "El enlace ya fue utilizado o expiró"));
    }

    public async Task<Result<object>> Resend(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 255)
            return Result<object>.Success(null!);

        var user = await _repository.GetTrackedByEmail(
            email.Trim().ToLowerInvariant(),
            cancellationToken
        );
        if (user == null || user.EmailVerificado)
            return Result<object>.Success(null!);

        await Issue(user, cancellationToken);
        return Result<object>.Success(null!);
    }
}
