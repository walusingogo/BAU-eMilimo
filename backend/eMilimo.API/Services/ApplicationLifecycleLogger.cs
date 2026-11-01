using eMilimo.Core.Entities;

namespace eMilimo.API.Services;

public class ApplicationLifecycleLogger : IHostedService
{
    private readonly ISystemLogWriter _writer;private readonly IWebHostEnvironment _environment;private readonly IServiceScopeFactory _scopeFactory;private readonly ILogger<ApplicationLifecycleLogger> _logger;
    public ApplicationLifecycleLogger(ISystemLogWriter writer,IWebHostEnvironment environment,IServiceScopeFactory scopeFactory,ILogger<ApplicationLifecycleLogger> logger){_writer=writer;_environment=environment;_scopeFactory=scopeFactory;_logger=logger;}
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _writer.WriteAsync(new SystemLog{Category="System",Process="application",Action="Startup",Level="Information",CorrelationId=Guid.NewGuid().ToString("N"),Message=$"eMilimo API started in {_environment.EnvironmentName}.",Details=$"ContentRoot={_environment.ContentRootPath}"},cancellationToken);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var state = await scope.ServiceProvider.GetRequiredService<ILicenseValidationService>().GetStateAsync();
            var result = state.IsValid ? "VALID" : "INVALID";
            var details = $"Result={result}; Status={state.Status}; Installed={state.Installed}; Authentic={state.IsAuthentic}; Trial={state.IsTrial}; Perpetual={state.IsPerpetual}; ExpiresAtUtc={state.ExpiresAtUtc?.ToString("O") ?? "none"}";
            var message = $"License validation after startup: {result} (status: {state.Status}).";
            if (state.IsValid) _logger.LogInformation("{Message} {Details}", message, details);
            else _logger.LogWarning("{Message} {Details}", message, details);
            await _writer.WriteAsync(new SystemLog{Category="Security",Process="license",Action="StartupValidation",Level=state.IsValid ? "Information" : "Warning",CorrelationId=Guid.NewGuid().ToString("N"),Message=message,Details=details},cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "License validation after startup failed.");
            await _writer.WriteAsync(new SystemLog{Category="Security",Process="license",Action="StartupValidation",Level="Error",CorrelationId=Guid.NewGuid().ToString("N"),Message="License validation after startup failed.",Details=ex.Message},cancellationToken);
        }
    }
    public Task StopAsync(CancellationToken cancellationToken)=>_writer.WriteAsync(new SystemLog{Category="System",Process="application",Action="Shutdown",Level="Information",CorrelationId=Guid.NewGuid().ToString("N"),Message="eMilimo API stopped."},cancellationToken);
}
