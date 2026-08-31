using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace IMT_Reservas.Server.Infrastructure.Config;

public static class RequestLimits
{
    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, token) =>
        {
            var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                ? Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)) : 60;
            context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                title = "Demasiadas solicitudes",
                status = 429,
                detail = $"Espera {seconds} segundos antes de volver a intentar.",
            }, token);
        };
        options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
            }));
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var identity = context.User.Identity?.IsAuthenticated == true
                ? "user:" + context.User.Identity.Name
                : "ip:" + context.Connection.RemoteIpAddress;
            return RateLimitPartition.GetSlidingWindowLimiter(identity, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 180, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6,
                QueueLimit = 0,
            });
        });
    }
}
