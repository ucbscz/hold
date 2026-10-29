using IMT_Reservas.Server.Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Modules.Notificaciones.Infrastructure;

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
