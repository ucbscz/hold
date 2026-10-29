using UCB_Hold.Server.Modules.Notificaciones.Application;
using UCB_Hold.Server.Modules.Notificaciones.Infrastructure;

namespace UCB_Hold.Server.Modules.Notificaciones;

public static class NotificacionesModule
{
    public static IServiceCollection AddNotificacionesModule(this IServiceCollection services)
    {
        services.AddScoped<NotificacionRepository>();
        return services.AddScoped<NotificacionService>();
    }
}
