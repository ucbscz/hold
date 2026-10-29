using IMT_Reservas.Server.Modules.Notificaciones.Application;
using IMT_Reservas.Server.Modules.Notificaciones.Infrastructure;

namespace IMT_Reservas.Server.Modules.Notificaciones;

public static class NotificacionesModule
{
    public static IServiceCollection AddNotificacionesModule(this IServiceCollection services)
    {
        services.AddScoped<NotificacionRepository>();
        return services.AddScoped<NotificacionService>();
    }
}
