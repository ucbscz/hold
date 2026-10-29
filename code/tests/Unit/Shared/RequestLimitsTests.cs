using System.Net;
using System.Security.Claims;
using FluentAssertions;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
public class RequestLimitsTests
{
    [Test]
    public void LimitsRequestsPerIdentityWithoutBlockingOtherUsers()
    {
        var options = new RateLimiterOptions();
        RequestLimits.Configure(options);
        using var limiter = options.GlobalLimiter!;
        var first = Context("first");
        for (var i = 0; i < 180; i++) { using var allowed = limiter.AttemptAcquire(first); allowed.IsAcquired.Should().BeTrue(); }
        using var denied = limiter.AttemptAcquire(first);
        denied.IsAcquired.Should().BeFalse();
        using var other = limiter.AttemptAcquire(Context("second"));
        other.IsAcquired.Should().BeTrue();
        options.RejectionStatusCode.Should().Be(429);
    }

    [Test]
    public void AnonymousRequestsShareTheIpBudget()
    {
        var options = new RateLimiterOptions();
        RequestLimits.Configure(options);
        using var limiter = options.GlobalLimiter!;
        var first = Context(null);
        using var budget = limiter.AttemptAcquire(first, 180);
        using var denied = limiter.AttemptAcquire(Context(null));
        denied.IsAcquired.Should().BeFalse();
    }

    private static DefaultHttpContext Context(string? name)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        if (name != null) context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, name) }, "test"));
        return context;
    }
}
