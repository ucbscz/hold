using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using IMT_Reservas.Server.Modules.Inventario.Domain;
using IMT_Reservas.Server.Modules.Mantenimientos.Domain;
using IMT_Reservas.Server.Shared.Persistence;

namespace IMT_Reservas.Server.Modules.Mantenimientos.Infrastructure;

internal sealed class EmpresaMantenimientoConfiguration : IEntityTypeConfiguration<EmpresaMantenimiento>
{
    public void Configure(EntityTypeBuilder<EmpresaMantenimiento> entity)
    {
        entity.ToTable("empresas_mantenimiento");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_empresa_mantenimiento");
        entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(DatabaseColumns.Nombre);
        entity.Property(e => e.Nit).HasMaxLength(20).HasColumnName("nit");
        entity.Property(e => e.Direccion).HasMaxLength(512).HasColumnName("direccion");
        entity.Property(e => e.Telefono).HasMaxLength(64).HasColumnName("telefono");
        entity.Property(e => e.NombreResponsable).HasMaxLength(64).HasColumnName("nombre_responsable");
        entity.Property(e => e.ApellidoResponsable).HasMaxLength(64).HasColumnName("apellido_responsable");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasIndex(e => new { e.Nombre, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

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
