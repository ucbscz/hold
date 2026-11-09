using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Modules.Usuarios.Domain;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Usuarios.Infrastructure;

internal sealed class CarreraConfiguration : IEntityTypeConfiguration<Carrera>
{
    public void Configure(EntityTypeBuilder<Carrera> entity)
    {
        entity.ToTable("carreras");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_carrera");
        entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(DatabaseColumns.Nombre);
        entity.Property(e => e.Codigo).IsRequired().HasMaxLength(10).HasColumnName("codigo");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasIndex(e => new { e.Nombre, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> entity)
    {
        entity.ToTable("usuarios");
        entity.HasKey(e => e.Carnet);
        entity.Ignore(e => e.Id);
        entity.Property(e => e.Carnet).HasMaxLength(20).HasColumnName("carnet");
        entity.Property(e => e.Nombre).IsRequired().HasMaxLength(64).HasColumnName(DatabaseColumns.Nombre);
        entity.Property(e => e.ApellidoPaterno).IsRequired().HasMaxLength(64).HasColumnName("apellido_paterno");
        entity.Property(e => e.ApellidoMaterno).IsRequired().HasMaxLength(64).HasColumnName("apellido_materno");
        entity.Property(e => e.Email).IsRequired().HasMaxLength(255).HasColumnName("email");
        entity.Property(e => e.Contrasena).IsRequired().HasMaxLength(72).HasColumnName("contrasena");
        entity.Property(e => e.Telefono).IsRequired().HasMaxLength(32).HasColumnName("telefono");
        entity.Property(e => e.TelefonoReferencia).HasMaxLength(32).HasColumnName("telefono_referencia");
        entity.Property(e => e.NombreReferencia).HasMaxLength(32).HasColumnName("nombre_referencia");
        entity.Property(e => e.EmailReferencia).HasMaxLength(255).HasColumnName("email_referencia");
        entity.Property(e => e.Rol).HasColumnType(PostgresEnumNames.TipoUsuario).HasDefaultValue(TipoUsuario.Estudiante).HasSentinel((TipoUsuario)(-1)).HasColumnName("rol");
        entity.Property(e => e.IdCarrera).HasColumnName("id_carrera");
        entity.Property(e => e.Bloqueado).HasColumnName("bloqueado");
        entity.Property(e => e.MotivoBloqueo).HasColumnName("motivo_bloqueo");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.Property(e => e.ImagenPerfil).HasColumnName("imagen_perfil");
        entity.Property(e => e.ImagenFrenteCarnet).HasColumnName("imagen_frente_carnet");
        entity.Property(e => e.ImagenAtrasCarnet).HasColumnName("imagen_atras_carnet");
        entity.Property(e => e.ImagenFirma).HasColumnName("imagen_firma");
        entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
        entity.Property(e => e.RefreshTokenExpiry).HasColumnName("refresh_token_expiry");
        entity.Property(e => e.EmailVerificado).HasColumnName("email_verificado").HasDefaultValue(true);
        entity.Property(e => e.IdentidadExternaId).HasMaxLength(255).HasColumnName("google_id");
        entity.Property(e => e.TokenVerificacionHash).HasMaxLength(64).HasColumnName("token_verificacion_hash");
        entity.Property(e => e.TokenVerificacionExpira).HasColumnName("token_verificacion_expira");
        entity.HasOne<Carrera>().WithMany().HasForeignKey(e => e.IdCarrera).IsRequired();
        entity.HasIndex(e => new { e.Nombre, e.EstadoEliminado });
        entity.HasIndex(e => e.Email).IsUnique();
        entity.HasIndex(e => e.RefreshToken);
        entity.HasIndex(e => e.IdentidadExternaId).IsUnique();
        entity.HasIndex(e => e.TokenVerificacionHash).IsUnique();
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class CodigoAutenticacionConfiguration : IEntityTypeConfiguration<CodigoAutenticacion>
{
    public void Configure(EntityTypeBuilder<CodigoAutenticacion> entity)
    {
        entity.ToTable("codigos_autenticacion");
        entity.HasKey(e => e.Hash);
        entity.Property(e => e.Hash).HasMaxLength(64).HasColumnName("hash");
        entity.Property(e => e.Tipo).HasMaxLength(20).HasColumnName("tipo");
        entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
        entity.Property(e => e.IdentidadExternaId).HasMaxLength(255).HasColumnName("google_id");
        entity.Property(e => e.Nombre).HasMaxLength(64).HasColumnName("nombre");
        entity.Property(e => e.ApellidoPaterno).HasMaxLength(64).HasColumnName("apellido_paterno");
        entity.Property(e => e.ApellidoMaterno).HasMaxLength(64).HasColumnName("apellido_materno");
        entity.Property(e => e.Expira).HasColumnName("expira");
        entity.Property(e => e.Usado).HasColumnName("usado");
        entity.HasIndex(e => new { e.Expira, e.Usado });
    }
}
