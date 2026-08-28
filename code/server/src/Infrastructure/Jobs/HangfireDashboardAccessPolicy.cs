using Hangfire.Dashboard;

namespace IMT_Reservas.Server.Infrastructure.Jobs;

public sealed class HangfireDashboardAccessPolicy : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("administrador");
    }
}
