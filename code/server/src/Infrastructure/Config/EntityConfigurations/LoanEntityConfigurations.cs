using IMT_Reservas.Server.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMT_Reservas.Server.Infrastructure.Config.EntityConfigurations;

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
        entity.Property(e => e.Rol).HasColumnType("tipo_usuario").HasDefaultValue(TipoUsuario.Estudiante).HasSentinel((TipoUsuario)(-1)).HasColumnName("rol");
        entity.Property(e => e.IdCarrera).HasColumnName("id_carrera");
        entity.Property(e => e.Bloqueado).HasColumnName("bloqueado");
        entity.Property(e => e.MotivoBloqueo).HasColumnName("motivo_bloqueo");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.Property(e => e.ImagenFrenteCarnet).HasColumnName("imagen_frente_carnet");
        entity.Property(e => e.ImagenAtrasCarnet).HasColumnName("imagen_atras_carnet");
        entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
        entity.Property(e => e.RefreshTokenExpiry).HasColumnName("refresh_token_expiry");
        entity.HasOne<Carrera>().WithMany().HasForeignKey(e => e.IdCarrera).IsRequired();
        entity.HasIndex(e => new { e.Nombre, e.EstadoEliminado });
        entity.HasIndex(e => e.Email).IsUnique();
        entity.HasIndex(e => e.RefreshToken);
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class PrestamoConfiguration : IEntityTypeConfiguration<Prestamo>
{
    public void Configure(EntityTypeBuilder<Prestamo> entity)
    {
        entity.ToTable("prestamos");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_prestamo");
        entity.Property(e => e.FechaSolicitud).HasDefaultValueSql("now() AT TIME ZONE 'America/La_Paz'").HasColumnName("fecha_solicitud");
        entity.Property(e => e.FechaPrestamo).HasColumnName("fecha_prestamo");
        entity.Property(e => e.FechaPrestamoEsperada).IsRequired().HasColumnName("fecha_prestamo_esperada");
        entity.Property(e => e.FechaDevolucion).HasColumnName("fecha_devolucion");
        entity.Property(e => e.FechaDevolucionEsperada).IsRequired().HasColumnName("fecha_devolucion_esperada");
        entity.Property(e => e.Observacion).HasMaxLength(1024).HasColumnName("observacion");
        entity.Property(e => e.EstadoPrestamo).HasColumnType("estado_prestamo").HasDefaultValue(EstadoPrestamo.Pendiente).HasColumnName("estado_prestamo");
        entity.Property(e => e.IdContrato).HasColumnName("id_contrato");
        entity.Property(e => e.Carnet).HasMaxLength(20).HasColumnName("carnet");
        entity.Property(e => e.RecordatorioEnviado).HasColumnName("recordatorio_enviado").HasDefaultValue(false);
        entity.Property(e => e.DestinoPrestamo).HasMaxLength(50).HasColumnName("destino_prestamo").HasDefaultValue("Universidad");
        entity.Property(e => e.IdCarrera).HasColumnName("id_carrera");
        entity.Property(e => e.NombreMateria).HasMaxLength(255).HasColumnName("nombre_materia");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasOne(e => e.Usuario).WithMany().HasForeignKey(e => e.Carnet).IsRequired();
        entity.HasOne(e => e.Contrato).WithMany().HasForeignKey(e => e.IdContrato);
        entity.HasOne(e => e.Carrera).WithMany().HasForeignKey(e => e.IdCarrera);
        entity.HasIndex(e => e.IdContrato);
        entity.HasIndex(e => new { e.FechaPrestamoEsperada, e.FechaDevolucionEsperada, e.Carnet, e.EstadoEliminado });
        entity.HasIndex(e => new { e.Carnet, e.EstadoPrestamo, e.EstadoEliminado });
        entity.HasIndex(e => new { e.EstadoPrestamo, e.EstadoEliminado, e.FechaDevolucionEsperada });
        entity.HasIndex(e => new { e.EstadoPrestamo, e.EstadoEliminado, e.FechaPrestamoEsperada });
        entity.HasIndex(e => new { e.EstadoPrestamo, e.RecordatorioEnviado, e.EstadoEliminado, e.FechaDevolucionEsperada });
        entity.HasIndex(e => new { e.EstadoEliminado, e.FechaSolicitud, e.Id });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class ContratoConfiguration : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> entity)
    {
        entity.ToTable("contratos");
        entity.HasKey(e => e.Id);
        entity.Ignore(e => e.EstadoEliminado);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.ContratoHtml).HasColumnType("text").HasColumnName("contrato");
    }
}

internal sealed class DetallePrestamoConfiguration : IEntityTypeConfiguration<DetallePrestamo>
{
    public void Configure(EntityTypeBuilder<DetallePrestamo> entity)
    {
        entity.ToTable("detalles_prestamos");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_detalle_prestamo");
        entity.Property(e => e.IdPrestamo).HasColumnName("id_prestamo");
        entity.Property(e => e.IdEquipo).HasColumnName(DatabaseColumns.IdEquipo);
        entity.Property(e => e.IdGrupoEquipo).HasColumnName("id_grupo_equipo");
        entity.Property(e => e.EstadoEquipoRetorno).HasColumnType(DatabaseColumns.EstadoEquipo).HasColumnName("estado_equipo_retorno");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasOne<Prestamo>().WithMany().HasForeignKey(e => e.IdPrestamo).IsRequired();
        entity.HasOne<Equipo>().WithMany().HasForeignKey(e => e.IdEquipo).IsRequired(false);
        entity.HasOne<GrupoEquipo>().WithMany().HasForeignKey(e => e.IdGrupoEquipo).IsRequired();
        entity.HasIndex(e => new { e.IdPrestamo, e.EstadoEliminado });
        entity.HasIndex(e => e.IdEquipo);
        entity.HasIndex(e => new { e.IdGrupoEquipo, e.IdEquipo, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}
