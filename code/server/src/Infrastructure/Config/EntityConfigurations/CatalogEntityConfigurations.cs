using IMT_Reservas.Server.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMT_Reservas.Server.Infrastructure.Config.EntityConfigurations;

internal sealed class CarreraConfiguration : IEntityTypeConfiguration<Carrera>
{
    public void Configure(EntityTypeBuilder<Carrera> entity)
    {
        entity.ToTable("carreras");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_carrera");
        entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(DatabaseColumns.Nombre);
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasIndex(e => new { e.Nombre, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> entity)
    {
        entity.ToTable("categorias");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_categoria");
        entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(DatabaseColumns.Nombre);
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasIndex(e => new { e.Nombre, e.EstadoEliminado });
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

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

internal sealed class ConfiguracionSistemaConfiguration : IEntityTypeConfiguration<ConfiguracionSistema>
{
    public void Configure(EntityTypeBuilder<ConfiguracionSistema> entity)
    {
        entity.ToTable("configuraciones_sistema");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_configuracion");
        entity.Property(e => e.MontoMinimoContrato).HasColumnName("monto_minimo_contrato").HasColumnType("numeric(18,2)");
        entity.Property(e => e.HorarioInicioMinutos).HasColumnName("horario_inicio_minutos");
        entity.Property(e => e.HorarioFinMinutos).HasColumnName("horario_fin_minutos");
        entity.Property(e => e.Horarios).HasColumnName("horarios").HasColumnType("jsonb")
            .HasConversion(
                value => System.Text.Json.JsonSerializer.Serialize(value, (System.Text.Json.JsonSerializerOptions?)null),
                value => System.Text.Json.JsonSerializer.Deserialize<List<HorarioAtencion>>(value, (System.Text.Json.JsonSerializerOptions?)null) ?? new())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<HorarioAtencion>>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                value => value.Select(item => new HorarioAtencion { DiaSemana = item.DiaSemana, Fecha = item.Fecha, Abierto = item.Abierto, InicioMinutos = item.InicioMinutos, FinMinutos = item.FinMinutos }).ToList()));
        entity.Property(e => e.NombreJefeCarrera).HasColumnName("nombre_jefe_carrera").HasMaxLength(255);
        entity.Property(e => e.FirmaJefeCarreraBase64).HasColumnName("firma_jefe_carrera_base64");
        entity.Property(e => e.TiempoMinimoReservaMinutos).HasColumnName("tiempo_minimo_reserva_minutos");
        entity.Property(e => e.TiempoRecordatorioPrevioMinutos).HasColumnName("tiempo_recordatorio_previo_minutos");
        entity.Property(e => e.MinutosGraciaAtraso).HasColumnName("minutos_gracia_atraso");
        entity.Property(e => e.EstadoEliminado).HasColumnName(DatabaseColumns.EstadoEliminado);
        entity.HasData(ConfiguracionSeed.Default);
    }
}
