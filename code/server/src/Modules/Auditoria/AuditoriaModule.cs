using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Auditoria.Infrastructure;

namespace UCB_Hold.Server.Modules.Auditoria;

public static class AuditoriaModule
{
    public static IServiceCollection AddAuditoriaModule(this IServiceCollection services)
    {
        services.AddScoped<AuditLogRepository>();
        return services.AddScoped<AuditLogService>();
    }
}
