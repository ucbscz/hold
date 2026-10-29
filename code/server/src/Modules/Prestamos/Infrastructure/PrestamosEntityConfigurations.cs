using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using IMT_Reservas.Server.Modules.Inventario.Domain;
using IMT_Reservas.Server.Modules.Prestamos.Domain;
using IMT_Reservas.Server.Shared.Persistence;

namespace IMT_Reservas.Server.Modules.Prestamos.Infrastructure;

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

internal sealed class PrestamoConfiguration : IEntityTypeConfiguration<Prestamo>
{
    public void Configure(EntityTypeBuilder<Prestamo> entity)
    {
        entity.ToTable("prestamos");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_prestamo");
        entity.Property(e => e.FechaSolicitud).ValueGeneratedOnAdd().HasColumnName("fecha_solicitud");
        entity.Property(e => e.FechaPrestamo).HasColumnName("fecha_prestamo");
        entity.Property(e => e.FechaPrestamoEsperada).IsRequired().HasColumnName("fecha_prestamo_esperada");
        entity.Property(e => e.FechaDevolucion).HasColumnName("fecha_devolucion");
        entity.Property(e => e.FechaDevolucionEsperada).IsRequired().HasColumnName("fecha_devolucion_esperada");
        entity.Property(e => e.Observacion).HasMaxLength(1024).HasColumnName("observacion");
        entity.Property(e => e.MotivoRechazo).HasMaxLength(1024).HasColumnName("motivo_rechazo");
        entity.Property(e => e.AutorizadoPor).HasMaxLength(255).HasColumnName("autorizado_por");
        entity.Property(e => e.EntregadoPor).HasMaxLength(255).HasColumnName("entregado_por");
        entity.Property(e => e.EstadoPrestamo).HasColumnType(PostgresEnumNames.EstadoPrestamo).HasDefaultValue(EstadoPrestamo.Pendiente).HasColumnName("estado_prestamo");
        entity.Property(e => e.IdContrato).HasColumnName("id_contrato");
        entity.Property(e => e.Carnet).HasMaxLength(20).HasColumnName("carnet");
        entity.Property(e => e.RecordatorioEnviado).HasColumnName("recordatorio_enviado").HasDefaultValue(false);
        entity.Property(e => e.DestinoPrestamo).HasMaxLength(50).HasColumnName("destino_prestamo").HasDefaultValue(DestinoUso.Universidad);
        entity.Property(e => e.IdCarrera).HasColumnName("id_carrera");
        entity.Property(e => e.NombreMateria).HasMaxLength(255).HasColumnName("nombre_materia");
        entity.Property(e => e.Guardado).HasColumnName("guardado").HasDefaultValue(false);
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
