using Hangfire.Dashboard;
using IMT_Reservas.Server.Shared.Security;

namespace IMT_Reservas.Server.Shared.Jobs;

public sealed class HangfireDashboardAccessPolicy : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole(Permisos.Administrador);
    }
}
