using FluentValidation;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Application.Features.Usuario;

public class UsuarioValidator : AbstractValidator<UsuarioDto>
{
    public UsuarioValidator(ApplicationDbContext dbContext)
    {
        RuleFor(usuario => usuario.Rol)
            .Must(rol => string.IsNullOrWhiteSpace(rol) || new[]
            { "estudiante", "docente", "administrativo", "administrador", "administrador_laboratorio" }.Contains(rol))
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
            .Must(email => email != null && email.EndsWith("@ucb.edu.bo", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Debes usar un correo institucional @ucb.edu.bo")
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
            .Must(image => image == null || image.Length <= 1024 * 1024)
            .WithMessage("La foto de perfil no puede superar 1 MB");
        RuleFor(usuario => usuario.ImagenFrenteCarnet)
            .Must(image => image == null || image.Length <= 5 * 1024 * 1024)
            .WithMessage("La imagen frontal del carnet no puede superar 5 MB");
        RuleFor(usuario => usuario.ImagenAtrasCarnet)
            .Must(image => image == null || image.Length <= 5 * 1024 * 1024)
            .WithMessage("La imagen posterior del carnet no puede superar 5 MB");
        RuleFor(usuario => usuario.ImagenFirma)
            .Must(image => image == null || image.Length <= 1024 * 1024)
            .WithMessage("La firma no puede superar 1 MB");

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
