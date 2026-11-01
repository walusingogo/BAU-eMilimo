using eMilimo.API.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace eMilimo.API.Tests.Middleware;

public sealed class LicenseEnforcementMiddlewareTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/setup")]
    [InlineData("/main.js")]
    [InlineData("/images/bau-logo.png")]
    public async Task BrowserShellAndStaticRoutesAreNeverBlockedByLicenseEnforcement(string path)
    {
        var nextCalled = false;
        var middleware = new LicenseEnforcementMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        await middleware.InvokeAsync(context, null!, null!);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }
}
