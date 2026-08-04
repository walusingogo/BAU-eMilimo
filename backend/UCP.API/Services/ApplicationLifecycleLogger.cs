using UCP.Core.Entities;

namespace UCP.API.Services;

public class ApplicationLifecycleLogger : IHostedService
{
    private readonly ISystemLogWriter _writer;private readonly IWebHostEnvironment _environment;
    public ApplicationLifecycleLogger(ISystemLogWriter writer,IWebHostEnvironment environment){_writer=writer;_environment=environment;}
    public Task StartAsync(CancellationToken cancellationToken)=>_writer.WriteAsync(new SystemLog{Category="System",Process="application",Action="Startup",Level="Information",CorrelationId=Guid.NewGuid().ToString("N"),Message=$"UCP API started in {_environment.EnvironmentName}.",Details=$"ContentRoot={_environment.ContentRootPath}"},cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken)=>_writer.WriteAsync(new SystemLog{Category="System",Process="application",Action="Shutdown",Level="Information",CorrelationId=Guid.NewGuid().ToString("N"),Message="UCP API stopped."},cancellationToken);
}
