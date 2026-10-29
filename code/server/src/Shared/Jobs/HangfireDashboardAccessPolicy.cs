using Hangfire.Dashboard;
using UCB_Hold.Server.Shared.Security;

namespace UCB_Hold.Server.Shared.Jobs;

public sealed class HangfireDashboardAccessPolicy : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole(Permisos.Administrador);
    }
}
