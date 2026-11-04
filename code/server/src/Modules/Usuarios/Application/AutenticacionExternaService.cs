using System.Security.Claims;
using Ardalis.Result;
using UCB_Hold.Server.Modules.Usuarios.Infrastructure;

namespace UCB_Hold.Server.Modules.Usuarios.Application;

public sealed class AutenticacionExternaService
{
    private readonly CodigoExternoService _codes;
    private readonly UsuarioAuthRepository _users;
    private readonly SesionService _sesiones;
    private readonly InstitutionalEmailPolicy _institutionalEmail;

    public AutenticacionExternaService(
        CodigoExternoService codes,
        UsuarioAuthRepository users,
        SesionService sesiones,
        InstitutionalEmailPolicy institutionalEmail
    )
    {
        _codes = codes;
        _users = users;
        _sesiones = sesiones;
        _institutionalEmail = institutionalEmail;
    }

    public async Task<Result<string>> Begin(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken
    )
    {
        var identidadExternaId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email)?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(identidadExternaId) || !_institutionalEmail.Accepts(email))
            return Result<string>.Unauthorized($"Debes usar una cuenta institucional {_institutionalEmail.Suffix}");

        var (user, _) = await _users.GetByEmailWithCarrera(email!, cancellationToken);
        var type = user == null ? TipoCodigoExterno.Registro : TipoCodigoExterno.Login;
        if (user != null)
        {
            if (!string.IsNullOrWhiteSpace(user.IdentidadExternaId) && user.IdentidadExternaId != identidadExternaId)
                return Result<string>.Unauthorized("El correo ya está vinculado con otra cuenta institucional");
            await _users.LinkExternal(user.Carnet, identidadExternaId, cancellationToken);
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
            identidadExternaId,
            name,
            surnames.FirstOrDefault() ?? string.Empty,
            surnames.Skip(1).FirstOrDefault() ?? string.Empty,
            cancellationToken
        );
        return Result<string>.Success(code);
    }

    public async Task<Result<ExternalAuthDto>> Exchange(
        string token,
        CancellationToken cancellationToken
    )
    {
        var code = await _codes.Get(token, cancellationToken);
        if (code == null)
            return Result<ExternalAuthDto>.Unauthorized("El acceso con la cuenta institucional expiró o ya fue utilizado");

        if (code.Tipo == TipoCodigoExterno.Registro)
            return Result<ExternalAuthDto>.Success(new ExternalAuthDto
            {
                RequiereRegistro = true,
                CodigoRegistro = token,
                Email = code.Email,
                Nombre = code.Nombre,
                ApellidoPaterno = code.ApellidoPaterno,
                ApellidoMaterno = code.ApellidoMaterno,
            });

        if (!await _codes.Consume(token, cancellationToken))
            return Result<ExternalAuthDto>.Unauthorized("El acceso con la cuenta institucional ya fue utilizado");

        var (user, careerName) = await _users.GetByEmailWithCarrera(code.Email, cancellationToken);
        if (user == null || user.IdentidadExternaId != code.IdentidadExternaId)
            return Result<ExternalAuthDto>.Unauthorized("No se pudo vincular la cuenta institucional");

        return Result<ExternalAuthDto>.Success(new ExternalAuthDto
        {
            Sesion = await _sesiones.Issue(user, careerName, cancellationToken),
        });
    }
}
