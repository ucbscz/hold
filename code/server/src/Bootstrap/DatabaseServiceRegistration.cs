using Microsoft.EntityFrameworkCore;
using Npgsql;
using IMT_Reservas.Server.Modules.Inventario.Domain;
using IMT_Reservas.Server.Modules.Prestamos.Domain;
using IMT_Reservas.Server.Modules.Usuarios.Domain;
using IMT_Reservas.Server.Shared.Configuration;
using IMT_Reservas.Server.Shared.Persistence;
using IMT_Reservas.Server.Shared.Persistence.RowLevelSecurity;

namespace IMT_Reservas.Server.Bootstrap;

public static class DatabaseServiceRegistration
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(
            configuration.GetConnectionString(ConfigurationKeys.PostgreSql)
        );
        dataSourceBuilder.MapEnum<EstadoPrestamo>(PostgresEnumNames.EstadoPrestamo);
        dataSourceBuilder.MapEnum<TipoUsuario>(PostgresEnumNames.TipoUsuario);
        dataSourceBuilder.MapEnum<EstadoEquipo>(PostgresEnumNames.EstadoEquipo);
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);
        services.AddDbContext<ApplicationDbContext>(
            (provider, options) =>
                options
                    .UseNpgsql(dataSource)
                    .AddInterceptors(
                        new RowLevelSecurityInterceptor(
                            provider.GetRequiredService<IHttpContextAccessor>()
                        )
                    )
        );
        return services;
    }
}
