using FluentValidation;
using IMT_Reservas.Server.Application.Security;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Application.Features.Usuario;

public class UsuarioValidator : AbstractValidator<UsuarioDto>
{
    private const int MaxProfileImageBytes = 1024 * 1024;
    private const int MaxCarnetImageBytes = 5 * 1024 * 1024;

    private static readonly string[] Roles = Enum.GetNames<TipoUsuario>()
        .Select(rol => rol.ToLowerInvariant())
        .ToArray();

    public UsuarioValidator(ApplicationDbContext dbContext)
    {
        RuleFor(usuario => usuario.Rol)
            .Must(rol => string.IsNullOrWhiteSpace(rol) || Roles.Contains(rol))
            .WithMessage("Rol de usuario no reconocido");
        RuleFor(usuario => usuario.Carnet)
            .NotEmpty()
            .WithMessage("Carnet requerido")
            .MaximumLength(20)
            .WithMessage("Carnet no puede superar 20 caracteres");
        RuleFor(usuario => usuario.Nombre)
            .NotEmpty()
            .WithMessage("Nombre requerido")
            .MaximumLength(64)
            .WithMessage("Nombre no puede superar 64 caracteres");
        RuleFor(usuario => usuario.ApellidoPaterno)
            .NotEmpty()
            .WithMessage("Apellido paterno requerido")
            .MaximumLength(64)
            .WithMessage("Apellido paterno no puede superar 64 caracteres");
        RuleFor(usuario => usuario.ApellidoMaterno)
            .MaximumLength(64)
            .WithMessage("Apellido materno no puede superar 64 caracteres");
        RuleFor(usuario => usuario.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Email inválido")
            .Must(CorreoInstitucional.Es)
            .WithMessage($"Debes usar un correo institucional {CorreoInstitucional.Dominio}")
            .MaximumLength(255)
            .WithMessage("Email no puede superar 255 caracteres");
        RuleFor(usuario => usuario.Contrasena)
            .Cascade(CascadeMode.Stop)
            .MinimumLength(8)
            .WithMessage("Contraseña mínimo 8 caracteres")
            .Matches("[A-Z]")
            .WithMessage("Contraseña debe tener al menos una mayúscula")
            .Matches("[0-9]")
            .WithMessage("Contraseña debe tener al menos un número")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage("Contraseña debe tener al menos un carácter especial")
            .MaximumLength(72)
            .WithMessage("Contraseña no puede superar 72 caracteres")
            .When(usuario => !string.IsNullOrWhiteSpace(usuario.Contrasena));

        RuleFor(usuario => usuario.Telefono)
            .MaximumLength(32)
            .WithMessage("Teléfono no puede superar 32 caracteres");
        RuleFor(usuario => usuario.TelefonoReferencia)
            .MaximumLength(32)
            .WithMessage("Teléfono de referencia no puede superar 32 caracteres");
        RuleFor(usuario => usuario.NombreReferencia)
            .MaximumLength(32)
            .WithMessage("Nombre de referencia no puede superar 32 caracteres");
        RuleFor(usuario => usuario.EmailReferencia)
            .MaximumLength(255)
            .WithMessage("Email de referencia no puede superar 255 caracteres");
        RuleFor(usuario => usuario.MotivoBloqueo)
            .MaximumLength(1024)
            .WithMessage("Motivo de bloqueo no puede superar 1024 caracteres");
        RuleFor(usuario => usuario.ImagenPerfil)
            .Must(image => AllowedImage.IsValid(image, MaxProfileImageBytes))
            .WithMessage("La foto de perfil debe ser JPG, PNG o WebP de hasta 1 MB");
        RuleFor(usuario => usuario.ImagenFrenteCarnet)
            .Must(image => AllowedImage.IsValid(image, MaxCarnetImageBytes))
            .WithMessage("La imagen frontal del carnet debe ser JPG, PNG o WebP de hasta 5 MB");
        RuleFor(usuario => usuario.ImagenAtrasCarnet)
            .Must(image => AllowedImage.IsValid(image, MaxCarnetImageBytes))
            .WithMessage("La imagen posterior del carnet debe ser JPG, PNG o WebP de hasta 5 MB");
        RuleFor(usuario => usuario.ImagenFirma)
            .Must(image => AllowedImage.IsValid(image, MaxProfileImageBytes))
            .WithMessage("La firma debe ser JPG, PNG o WebP de hasta 1 MB");

        RuleFor(usuario => usuario.IdCarrera)
            .MustAsync(
                async (id, cancellationToken) =>
                    await dbContext.Carreras.AnyAsync(
                        c => c.Id == id && !c.EstadoEliminado,
                        cancellationToken
                    )
            )
            .When(usuario => (usuario.IdCarrera ?? 0) > 0)
            .WithMessage("Carrera no existe");
    }
}
