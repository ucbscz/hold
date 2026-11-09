using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Auditoria.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Auditoria;

public static class AuditoriaModule
{
    public static IServiceCollection AddAuditoriaModule(this IServiceCollection services)
    {
        services.AddScoped<AuditLogRepository>();
        services.AddScoped<AuditLogService>();
        return services.AddScoped<IAuditTrail>(sp => sp.GetRequiredService<AuditLogService>());
    }
}
