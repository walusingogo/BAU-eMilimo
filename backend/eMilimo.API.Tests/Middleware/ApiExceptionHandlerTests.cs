using System.Text.Json;
using eMilimo.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace eMilimo.API.Tests.Middleware;

public sealed class ApiExceptionHandlerTests
{
    [Theory]
    [InlineData(typeof(KeyNotFoundException), StatusCodes.Status404NotFound)]
    [InlineData(typeof(UnauthorizedAccessException), StatusCodes.Status403Forbidden)]
    [InlineData(typeof(DbUpdateConcurrencyException), StatusCodes.Status409Conflict)]
    [InlineData(typeof(ArgumentException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status400BadRequest)]
    public async Task MapsExpectedExceptionsToProblemDetails(Type exceptionType, int expectedStatus)
    {
        var (handler, context) = CreateSubject();
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Domain message")!;

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        var body = await ReadBody(context);
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("message").GetString()));
        Assert.Equal("test-correlation", body.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task UnexpectedExceptionsDoNotExposeInternalDetails()
    {
        var (handler, context) = CreateSubject();

        await handler.TryHandleAsync(context, new Exception("sensitive database detail"), CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var json = await ReadBody(context);
        var body = json.RootElement.GetRawText();
        Assert.DoesNotContain("sensitive database detail", body);
        Assert.Contains("test-correlation", body);
    }

    private static (ApiExceptionHandler Handler, DefaultHttpContext Context) CreateSubject()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddProblemDetails();
        var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = provider,
            TraceIdentifier = "test-correlation"
        };
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        var details = provider.GetRequiredService<IProblemDetailsService>();
        return (new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance, details), context);
    }

    private static async Task<JsonDocument> ReadBody(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
