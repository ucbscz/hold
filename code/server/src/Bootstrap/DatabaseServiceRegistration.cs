using Microsoft.EntityFrameworkCore;
using Npgsql;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Modules.Prestamos.Domain;
using UCB_Hold.Server.Modules.Usuarios.Domain;
using UCB_Hold.Server.Shared.Configuration;
using UCB_Hold.Server.Shared.Persistence;
using UCB_Hold.Server.Shared.Persistence.RowLevelSecurity;

namespace UCB_Hold.Server.Bootstrap;

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
