using IMT_Reservas.Server.Modules.Auditoria.Application;
using IMT_Reservas.Server.Modules.Auditoria.Infrastructure;

namespace IMT_Reservas.Server.Modules.Auditoria;

public static class AuditoriaModule
{
    public static IServiceCollection AddAuditoriaModule(this IServiceCollection services)
    {
        services.AddScoped<AuditLogRepository>();
        return services.AddScoped<AuditLogService>();
    }
}
