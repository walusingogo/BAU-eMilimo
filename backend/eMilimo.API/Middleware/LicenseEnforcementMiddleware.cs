using Microsoft.EntityFrameworkCore;
using eMilimo.API.Services;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Middleware;
public sealed class LicenseEnforcementMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILicenseValidationService licenses, AppDbContext db)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // The browser shell, static resources and first-run page must remain reachable so an
        // administrator can install or renew a licence. Licence enforcement belongs at the API boundary.
        if (!path.Equals("/api", StringComparison.Ordinal) && !path.StartsWith("/api/", StringComparison.Ordinal))
        {
            await next(context);
            return;
        }
        if (path.StartsWith("/api/setup") || path.StartsWith("/api/auth/login") || path.StartsWith("/api/auth/client-settings"))
        {
            await next(context);
            return;
        }

        var state = await licenses.GetStateAsync();
        if (state.IsValid)
        {
            await next(context);
            return;
        }

        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var admin = Guid.TryParse(userIdClaim, out var id) &&
                    await db.Users.AnyAsync(x => x.Id == id && (x.Role == "Admin" || x.Role == "SuperAdmin"));
        var renewalPath = path.StartsWith("/api/license") || path.StartsWith("/api/auth/logout");
        if (admin && renewalPath)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
        await context.Response.WriteAsJsonAsync(new
        {
            message = "The system license is not active. Contact the system provider to renew access.",
            licenseStatus = state.Status,
            state.ContactEmail,
            state.ContactPhone
        });
    }
}
