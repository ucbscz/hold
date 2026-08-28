using IMT_Reservas.Server.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IMT_Reservas.Server.Infrastructure.Config;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Carrera> Carreras { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<GrupoEquipo> GruposEquipos { get; set; }
    public DbSet<Mueble> Muebles { get; set; }
    public DbSet<Gavetero> Gaveteros { get; set; }
    public DbSet<Equipo> Equipos { get; set; }
    public DbSet<Accesorio> Accesorios { get; set; }
    public DbSet<Componente> Componentes { get; set; }
    public DbSet<EmpresaMantenimiento> EmpresasMantenimiento { get; set; }
    public DbSet<Mantenimiento> Mantenimientos { get; set; }
    public DbSet<DetalleMantenimiento> DetallesMantenimientos { get; set; }
    public DbSet<Prestamo> Prestamos { get; set; }
    public DbSet<DetallePrestamo> DetallesPrestamos { get; set; }
    public DbSet<Contrato> Contratos { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notificacion> Notificaciones { get; set; }
    public DbSet<ConfiguracionSistema> ConfiguracionesSistema { get; set; }
    public DbSet<AvisoDisponibilidad> AvisosDisponibilidad { get; set; }
    public DbSet<ComentarioEquipo> ComentariosEquipos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresEnum<EstadoPrestamo>("estado_prestamo");
        modelBuilder.HasPostgresEnum<TipoUsuario>("tipo_usuario");
        modelBuilder.HasPostgresEnum<EstadoEquipo>("estado_equipo");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        ApplyUtcDateTimeConverter(modelBuilder);
    }

    private static void ApplyUtcDateTimeConverter(ModelBuilder modelBuilder)
    {
        var converter = new ValueConverter<DateTime, DateTime>(
            value => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        );

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entityType.GetProperties())
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(converter);
    }
}
