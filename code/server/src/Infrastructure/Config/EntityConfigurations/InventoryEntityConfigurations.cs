using IMT_Reservas.Server.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMT_Reservas.Server.Infrastructure.Config.EntityConfigurations;

internal sealed class GrupoEquipoConfiguration : IEntityTypeConfiguration<GrupoEquipo>
{
    public void Configure(EntityTypeBuilder<GrupoEquipo> entity)
    {
        entity.ToTable("grupos_equipos");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_grupo_equipo");
        entity.Property(e => e.Nombre).IsRequired().HasMaxLength(256).HasColumnName(DatabaseColumns.Nombre);
        entity.Property(e => e.Modelo).IsRequired().HasMaxLength(512).HasColumnName("modelo");
        entity.Property(e => e.Marca).IsRequired().HasMaxLength(256).HasColumnName("marca");
        entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(2048).HasColumnName(DatabaseColumns.Descripcion);
        entity.Property(e => e.UrlDataSheet).HasMaxLength(2048).HasColumnName("url_data_sheet");
        entity.Property(e => e.UrlImagen).IsRequired().HasMaxLength(2048).HasColumnName("url_imagen");
        entity.Property(e => e.CostoPromedio).HasPrecision(10, 2).HasColumnName("costo_promedio");
        entity.Property(e => e.Cantidad).HasDefaultValue(0).HasColumnName("cantidad");
        entity.Property(e => e.TiempoMaximoPrestamoDias).HasDefaultValue(7).HasColumnName("tiempo_max_prestamo_dias");
        entity.Property(e => e.IdCategoria).HasColumnName("id_categoria");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasOne(e => e.Categoria).WithMany().HasForeignKey(e => e.IdCategoria).IsRequired();
        entity.HasIndex(e => new { e.IdCategoria, e.Nombre, e.Modelo, e.Marca, e.EstadoEliminado });
        entity.HasIndex(e => new { e.Nombre, e.Modelo, e.Marca }).IsUnique();
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class MuebleConfiguration : IEntityTypeConfiguration<Mueble>
{
    public void Configure(EntityTypeBuilder<Mueble> entity)
    {
        entity.ToTable("muebles");
        entity.Property(e => e.IdAmbiente).HasColumnName("id_ambiente");
        entity.HasOne(e => e.Ambiente).WithMany().HasForeignKey(e => e.IdAmbiente).OnDelete(DeleteBehavior.Restrict);
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_mueble");
        entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(DatabaseColumns.Nombre);
        entity.Property(e => e.Tipo).HasMaxLength(255).HasColumnName("tipo");
        entity.Property(e => e.Ubicacion).HasMaxLength(255).HasColumnName("ubicacion");
        entity.Property(e => e.NumeroGaveteros).HasDefaultValue(0).HasColumnName("numero_gaveteros");
        entity.Property(e => e.Longitud).HasColumnName("longitud");
        entity.Property(e => e.Profundidad).HasColumnName("profundidad");
        entity.Property(e => e.Altura).HasColumnName("altura");
        entity.Property(e => e.Costo).HasColumnName("costo");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasIndex(e => new { e.Nombre, e.EstadoEliminado });
        entity.HasIndex(e => e.Nombre).IsUnique();
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class GaveteroConfiguration : IEntityTypeConfiguration<Gavetero>
{
    public void Configure(EntityTypeBuilder<Gavetero> entity)
    {
        entity.ToTable("gaveteros");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_gavetero");
        entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(DatabaseColumns.Nombre);
        entity.Property(e => e.Tipo).HasMaxLength(255).HasColumnName("tipo");
        entity.Property(e => e.IdMueble).HasColumnName("id_mueble");
        entity.Property(e => e.Longitud).HasColumnName("longitud");
        entity.Property(e => e.Profundidad).HasColumnName("profundidad");
        entity.Property(e => e.Altura).HasColumnName("altura");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasOne(e => e.Mueble).WithMany().HasForeignKey(e => e.IdMueble).IsRequired();
        entity.HasIndex(e => new { e.Nombre, e.IdMueble, e.EstadoEliminado });
        entity.HasIndex(e => e.Nombre).IsUnique();
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class EquipoConfiguration : IEntityTypeConfiguration<Equipo>
{
    public void Configure(EntityTypeBuilder<Equipo> entity)
    {
        entity.ToTable("equipos");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName(DatabaseColumns.IdEquipo);
        entity.Property(e => e.CodigoImt).IsRequired().HasColumnName("codigo_imt");
        entity.Property(e => e.CodigoUcb).HasMaxLength(256).HasColumnName("codigo_ucb");
        entity.HasIndex(e => e.CodigoUcb).IsUnique().HasDatabaseName("unique_codigo_ucb");
        entity.Property(e => e.NumeroSerial).HasMaxLength(255).HasColumnName("numero_serial");
        entity.Property(e => e.IdAmbiente).HasColumnName("id_ambiente");
        entity.HasOne(e => e.Ambiente).WithMany().HasForeignKey(e => e.IdAmbiente).OnDelete(DeleteBehavior.Restrict);
        entity.Property(e => e.Descripcion).HasMaxLength(2048).HasColumnName(DatabaseColumns.Descripcion);
        entity.Property(e => e.CostoReferencia).HasColumnName("costo_referencia");
        entity.Property(e => e.IdProcedencia).HasColumnName("id_procedencia");
        entity.HasOne(e => e.Procedencia).WithMany().HasForeignKey(e => e.IdProcedencia).OnDelete(DeleteBehavior.Restrict);
        entity.Property(e => e.FechaIngresoEquipo).HasDefaultValueSql("CURRENT_DATE").HasColumnName("fecha_ingreso_equipo");
        entity.Property(e => e.EstadoEquipo).HasColumnType(DatabaseColumns.EstadoEquipo).HasDefaultValue(EstadoEquipo.Operativo).HasColumnName(DatabaseColumns.EstadoEquipo);
        entity.Property(e => e.IdGrupoEquipo).HasColumnName("id_grupo_equipo");
        entity.Property(e => e.IdGavetero).HasColumnName("id_gavetero");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasOne(e => e.GrupoEquipo).WithMany().HasForeignKey(e => e.IdGrupoEquipo).IsRequired();
        entity.HasOne(e => e.Gavetero).WithMany().HasForeignKey(e => e.IdGavetero);
        entity.HasIndex(e => new { e.IdGrupoEquipo, e.CodigoImt, e.EstadoEliminado });
        entity.HasIndex(e => new { e.IdGrupoEquipo, e.EstadoEquipo, e.EstadoEliminado });
        entity.HasIndex(e => e.CodigoImt).IsUnique();
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class AccesorioConfiguration : IEntityTypeConfiguration<Accesorio>
{
    public void Configure(EntityTypeBuilder<Accesorio> entity)
    {
        entity.ToTable("accesorios");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_accesorio");
        entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(DatabaseColumns.Nombre);
        entity.Property(e => e.Descripcion).HasMaxLength(2048).HasColumnName(DatabaseColumns.Descripcion);
        entity.Property(e => e.Modelo).IsRequired().HasMaxLength(255).HasColumnName("modelo");
        entity.Property(e => e.UrlDataSheet).HasMaxLength(2048).HasColumnName("url_data_sheet");
        entity.Property(e => e.Tipo).HasMaxLength(255).HasColumnName("tipo");
        entity.Property(e => e.Precio).HasColumnName("precio");
        entity.Property(e => e.IdEquipo).HasColumnName(DatabaseColumns.IdEquipo);
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasOne<Equipo>().WithMany().HasForeignKey(e => e.IdEquipo).IsRequired();
        entity.HasIndex(e => new { e.Nombre, e.IdEquipo, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class ComponenteConfiguration : IEntityTypeConfiguration<Componente>
{
    public void Configure(EntityTypeBuilder<Componente> entity)
    {
        entity.ToTable("componentes");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_componente");
        entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(DatabaseColumns.Nombre);
        entity.Property(e => e.Descripcion).HasMaxLength(2048).HasColumnName(DatabaseColumns.Descripcion);
        entity.Property(e => e.Modelo).IsRequired().HasMaxLength(255).HasColumnName("modelo");
        entity.Property(e => e.UrlDataSheet).HasMaxLength(2048).HasColumnName("url_data_sheet");
        entity.Property(e => e.Tipo).HasMaxLength(255).HasColumnName("tipo");
        entity.Property(e => e.PrecioReferencia).HasColumnName("precio_referencia");
        entity.Property(e => e.IdEquipo).HasColumnName(DatabaseColumns.IdEquipo);
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasOne<Equipo>().WithMany().HasForeignKey(e => e.IdEquipo).IsRequired();
        entity.HasIndex(e => new { e.Nombre, e.IdEquipo, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}
