using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Middleware;

/// <summary>
/// Last-resort API exception boundary. Expected domain exceptions are translated
/// into one consistent RFC 9457 response and unexpected failures expose only a
/// correlation identifier; full exception details remain in server logs.
/// </summary>
public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail, logLevel) = exception switch
        {
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message,
                LogLevel.Information),
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Access denied",
                exception.Message,
                LogLevel.Warning),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "The record was changed",
                "This record changed while you were editing it. Reload and try again.",
                LogLevel.Warning),
            eMilimo.Core.Payments.PaymentConflictException => (
                StatusCodes.Status409Conflict,
                "Request conflict",
                exception.Message,
                LogLevel.Warning),
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request",
                exception.Message,
                LogLevel.Information),
            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "The operation could not be completed",
                exception.Message,
                LogLevel.Information),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "The request could not be completed. Contact support with the reference shown.",
                LogLevel.Error)
        };

        logger.Log(
            logLevel,
            exception,
            "API request failed with status {StatusCode}. CorrelationId: {CorrelationId}",
            status,
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["message"] = detail;
        problem.Extensions["correlationId"] = httpContext.TraceIdentifier;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
