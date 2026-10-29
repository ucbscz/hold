using UCB_Hold.Server.Shared.Web;
namespace UCB_Hold.Server.Shared.Security;

public static class SecurityHeaders
{
    private const string ApiContentSecurityPolicy =
        "default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'; form-action 'self';";

    private const string PageContentSecurityPolicy =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'; form-action 'self';";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(
            async (HttpContext context, Func<Task> next) =>
            {
                var headers = context.Response.Headers;
                headers.Append("X-Content-Type-Options", "nosniff");
                headers.Append("X-Frame-Options", "DENY");
                headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
                headers.Append("Cross-Origin-Opener-Policy", "same-origin");
                headers.Append("X-XSS-Protection", "0");
#pragma warning disable S7039
                headers.Append(
                    "Content-Security-Policy",
                    context.Request.Path.StartsWithSegments(ApiPaths.Api, StringComparison.OrdinalIgnoreCase)
                        ? ApiContentSecurityPolicy
                        : PageContentSecurityPolicy
                );
#pragma warning restore S7039
                await next();
            }
        );
}
