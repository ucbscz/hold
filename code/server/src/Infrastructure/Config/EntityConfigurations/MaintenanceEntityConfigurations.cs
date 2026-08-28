using IMT_Reservas.Server.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMT_Reservas.Server.Infrastructure.Config.EntityConfigurations;

internal sealed class MantenimientoConfiguration : IEntityTypeConfiguration<Mantenimiento>
{
    public void Configure(EntityTypeBuilder<Mantenimiento> entity)
    {
        entity.ToTable("mantenimientos");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_mantenimiento");
        entity.Property(e => e.Descripcion).HasMaxLength(2048).HasColumnName(DatabaseColumns.Descripcion);
        entity.Property(e => e.Costo).HasColumnName("costo");
        entity.Property(e => e.FechaMantenimiento).IsRequired().HasColumnName("fecha_mantenimiento");
        entity.Property(e => e.FechaFinalMantenimiento).IsRequired().HasColumnName("fecha_final_mantenimiento");
        entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasOne<EmpresaMantenimiento>().WithMany().HasForeignKey(e => e.IdEmpresa).IsRequired();
        entity.HasIndex(e => new { e.FechaMantenimiento, e.FechaFinalMantenimiento, e.IdEmpresa, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class DetalleMantenimientoConfiguration : IEntityTypeConfiguration<DetalleMantenimiento>
{
    public void Configure(EntityTypeBuilder<DetalleMantenimiento> entity)
    {
        entity.ToTable("detalles_mantenimientos");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_detalle_mantenimiento");
        entity.Property(e => e.TipoMantenimiento).HasMaxLength(256).HasColumnName("tipo_mantenimiento");
        entity.Property(e => e.Descripcion).HasMaxLength(2048).HasColumnName(DatabaseColumns.Descripcion);
        entity.Property(e => e.IdMantenimiento).HasColumnName("id_mantenimiento");
        entity.Property(e => e.IdEquipo).HasColumnName(DatabaseColumns.IdEquipo);
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasOne<Mantenimiento>().WithMany().HasForeignKey(e => e.IdMantenimiento).IsRequired();
        entity.HasOne<Equipo>().WithMany().HasForeignKey(e => e.IdEquipo).IsRequired();
        entity.HasIndex(e => new { e.IdMantenimiento, e.EstadoEliminado });
        entity.HasIndex(e => new { e.IdEquipo, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}
