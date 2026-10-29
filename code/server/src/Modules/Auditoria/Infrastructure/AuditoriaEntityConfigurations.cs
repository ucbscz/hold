using IMT_Reservas.Server.Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Modules.Auditoria.Infrastructure;

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
