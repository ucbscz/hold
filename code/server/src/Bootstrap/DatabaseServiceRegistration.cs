using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config.RowLevelSecurity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IMT_Reservas.Server.Infrastructure.Config;

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
