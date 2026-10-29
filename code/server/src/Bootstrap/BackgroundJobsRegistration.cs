using Hangfire;
using Hangfire.PostgreSql;
using UCB_Hold.Server.Modules.Mantenimientos.Application;
using UCB_Hold.Server.Modules.Prestamos.Application;
using UCB_Hold.Server.Shared.Configuration;
using UCB_Hold.Server.Shared.Jobs;

namespace UCB_Hold.Server.Bootstrap;

public static class BackgroundJobsRegistration
{
    private const string LoanStatusJobId = "estado-prestamo";
    private const string MaintenanceStatusJobId = "estado-mantenimiento";

    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        if (!IsEnabled(configuration, environment))
            return services;

        var connectionString = configuration.GetConnectionString(ConfigurationKeys.PostgreSql);
        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString))
        );
        return services.AddHangfireServer();
    }

    public static WebApplication UseBackgroundJobs(this WebApplication app)
    {
        if (!IsEnabled(app.Configuration, app.Environment))
            return app;

        app.UseHangfireDashboard(
            GetSettings(app.Configuration).DashboardPath,
            new DashboardOptions { Authorization = [new HangfireDashboardAccessPolicy()] }
        );
        RecurringJob.AddOrUpdate<EstadoPrestamoService>(
            LoanStatusJobId,
            job => job.Execute(CancellationToken.None),
            Cron.Minutely()
        );
        RecurringJob.AddOrUpdate<EstadoMantenimientoService>(
            MaintenanceStatusJobId,
            job => job.Execute(CancellationToken.None),
            Cron.Minutely()
        );
        return app;
    }

    private static bool IsEnabled(IConfiguration configuration, IHostEnvironment environment) =>
        GetSettings(configuration).Enabled ?? !environment.IsDevelopment();

    private static HangfireSettings GetSettings(IConfiguration configuration) =>
        configuration.GetSection(HangfireSettings.SectionName).Get<HangfireSettings>()
        ?? new HangfireSettings();
}
