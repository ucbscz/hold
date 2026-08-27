using IMT_Reservas.Server.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace IMT_Reservas.Server.Infrastructure.Config;

public sealed class DesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__PostgreSQL"
        ) ?? "Host=localhost;Database=design_time;Username=postgres";
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<EstadoPrestamo>("estado_prestamo");
        dataSourceBuilder.MapEnum<TipoUsuario>("tipo_usuario");
        dataSourceBuilder.MapEnum<EstadoEquipo>("estado_equipo");
        var dataSource = dataSourceBuilder.Build();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(dataSource)
            .Options;

        return new ApplicationDbContext(options);
    }
}
