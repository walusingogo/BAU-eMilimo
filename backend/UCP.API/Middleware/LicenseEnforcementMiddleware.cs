using Microsoft.EntityFrameworkCore;
using UCP.API.Services;
using UCP.Infrastructure.Data;

namespace UCP.API.Middleware;
public sealed class LicenseEnforcementMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context,ILicenseValidationService licenses,AppDbContext db)
    {
        var path=context.Request.Path.Value?.ToLowerInvariant()??"";
        if(path.StartsWith("/api/auth/login")||path.StartsWith("/api/auth/client-settings")||path.StartsWith("/swagger")||path.StartsWith("/images/")){await next(context);return;}
        var state=await licenses.GetStateAsync();if(state.IsValid){await next(context);return;}
        var userIdClaim=context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;var admin=false;if(Guid.TryParse(userIdClaim,out var id))admin=await db.Users.AnyAsync(x=>x.Id==id&&(x.Role=="Admin"||x.Role=="SuperAdmin"));
        var renewalPath=path.StartsWith("/api/license")||path.StartsWith("/api/auth/logout");if(admin&&renewalPath){await next(context);return;}
        context.Response.StatusCode=StatusCodes.Status402PaymentRequired;await context.Response.WriteAsJsonAsync(new{message="The system license is not active. Contact the system provider to renew access.",licenseStatus=state.Status,state.ContactEmail,state.ContactPhone});
    }
}
