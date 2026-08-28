using IMT_Reservas.Server.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMT_Reservas.Server.Infrastructure.Config.EntityConfigurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.ToTable("audit_logs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.AdminCarnet).HasColumnName("admin_carnet").HasMaxLength(20);
        entity.Property(e => e.AdminNombre).HasColumnName("admin_nombre");
        entity.Property(e => e.Accion).HasColumnName("accion").HasMaxLength(50);
        entity.Property(e => e.Entidad).HasColumnName("entidad").HasMaxLength(100);
        entity.Property(e => e.EntidadId).HasColumnName("entidad_id");
        entity.Property(e => e.Detalle).HasColumnName("detalle");
        entity.Property(e => e.Timestamp).HasColumnName("timestamp");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasIndex(e => new { e.AdminCarnet, e.Timestamp, e.EstadoEliminado });
        entity.HasIndex(e => new { e.Entidad, e.EntidadId, e.EstadoEliminado });
        entity.HasIndex(e => new { e.Entidad, e.Accion, e.Timestamp, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class ComentarioEquipoConfiguration : IEntityTypeConfiguration<ComentarioEquipo>
{
    public void Configure(EntityTypeBuilder<ComentarioEquipo> entity)
    {
        entity.ToTable("comentarios_equipos");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_comentario_equipo");
        entity.Property(e => e.IdGrupoEquipo).HasColumnName("id_grupo_equipo");
        entity.Property(e => e.IdComentarioPadre).HasColumnName("id_comentario_padre");
        entity.Property(e => e.CarnetUsuario).HasColumnName("carnet_usuario").HasMaxLength(20);
        entity.Property(e => e.Contenido).HasColumnName("contenido").HasMaxLength(1024);
        entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
        entity.Property(e => e.Likes).HasColumnName("likes").HasDefaultValue(0);
        entity.Property(e => e.LikedBy).HasColumnName("liked_by").HasColumnType("text").HasDefaultValue(string.Empty);
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasOne(e => e.GrupoEquipo).WithMany().HasForeignKey(e => e.IdGrupoEquipo).IsRequired();
        entity.HasOne(e => e.ComentarioPadre).WithMany().HasForeignKey(e => e.IdComentarioPadre).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Usuario).WithMany().HasForeignKey(e => e.CarnetUsuario).IsRequired();
        entity.HasIndex(e => new { e.IdGrupoEquipo, e.FechaCreacion, e.EstadoEliminado });
        entity.HasIndex(e => new { e.IdComentarioPadre, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> entity)
    {
        entity.ToTable("notificaciones");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_notificacion");
        entity.Property(e => e.CarnetUsuario).HasColumnName("carnet_usuario").HasMaxLength(20);
        entity.Property(e => e.Tipo).HasColumnName("tipo").HasMaxLength(50);
        entity.Property(e => e.Titulo).HasColumnName("titulo");
        entity.Property(e => e.Contenido).HasColumnName("contenido");
        entity.Property(e => e.Detalle).HasColumnName("detalle");
        entity.Property(e => e.Leido).HasColumnName("leido");
        entity.Property(e => e.FechaEnvio).HasColumnName("fecha_envio");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasIndex(e => new { e.CarnetUsuario, e.Leido, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class AvisoDisponibilidadConfiguration : IEntityTypeConfiguration<AvisoDisponibilidad>
{
    public void Configure(EntityTypeBuilder<AvisoDisponibilidad> entity)
    {
        entity.ToTable("avisos_disponibilidad");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_aviso");
        entity.Property(e => e.CarnetUsuario).HasColumnName("carnet_usuario").HasMaxLength(20);
        entity.Property(e => e.IdGrupoEquipo).HasColumnName("id_grupo_equipo");
        entity.Property(e => e.Fecha).HasColumnName("fecha");
        entity.Property(e => e.Cantidad).HasColumnName("cantidad");
        entity.Property(e => e.Notificado).HasColumnName("notificado");
        entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasIndex(e => new { e.Notificado, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}
