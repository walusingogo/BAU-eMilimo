using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UCP.Infrastructure.Data;

namespace UCP.API.Middleware
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionValidationMiddleware> _logger;

        public SessionValidationMiddleware(RequestDelegate next, ILogger<SessionValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            // Skip validation if the user is not authenticated.
            // This automatically excludes endpoints marked with [AllowAnonymous].
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            // Extract session ID from claims
            var sessionIdClaim = context.User.FindFirst("sessionId")?.Value;
            if (string.IsNullOrEmpty(sessionIdClaim))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid session");
                return;
            }

            // Get user ID from claims
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid user");
                return;
            }

            // Fetch user from database
            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.CurrentSessionId != sessionIdClaim)
            {
                _logger.LogWarning("Session mismatch for user {UserId}. Token session: {TokenSession}, DB session: {DbSession}",
                    userId, sessionIdClaim, user?.CurrentSessionId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Session expired or invalid. Please login again.");
                return;
            }

            // Session matches – proceed
            await _next(context);
        }
    }
}