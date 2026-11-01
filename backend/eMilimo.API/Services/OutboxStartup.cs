using BAU.Platform.Messaging.Hosting;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eMilimo.API.Services;

public interface IOutboxDatabaseMigrator
{
    Task EnsureReadyAsync(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken);
}

public interface IOutboxWorkerStarter
{
    Task StartAsync(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public static class OutboxStartup
{
    public static async Task EnsureDatabaseReadyAsync<TDbContext>(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}

public sealed class EfCoreOutboxDatabaseMigrator : IOutboxDatabaseMigrator
{
    public async Task EnsureReadyAsync(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        => await OutboxStartup.EnsureDatabaseReadyAsync<AppDbContext>(scopeFactory, cancellationToken);
}

public sealed class DefaultOutboxWorkerStarter : IOutboxWorkerStarter
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly OutboxWorkerOptions _options = new();
    private OutboxBackgroundService? _outboxBackgroundService;

    public DefaultOutboxWorkerStarter(ILoggerFactory loggerFactory)
        => _loggerFactory = loggerFactory;

    public Task StartAsync(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
    {
        _outboxBackgroundService = new OutboxBackgroundService(
            scopeFactory,
            _options,
            _loggerFactory.CreateLogger<OutboxBackgroundService>());

        return _outboxBackgroundService.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => _outboxBackgroundService is null
            ? Task.CompletedTask
            : _outboxBackgroundService.StopAsync(cancellationToken);
}

public sealed class DelayedOutboxWorkerHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOutboxDatabaseMigrator _databaseMigrator;
    private readonly IOutboxWorkerStarter _workerStarter;

    public DelayedOutboxWorkerHostedService(
        IServiceScopeFactory scopeFactory,
        IOutboxDatabaseMigrator databaseMigrator,
        IOutboxWorkerStarter workerStarter)
    {
        _scopeFactory = scopeFactory;
        _databaseMigrator = databaseMigrator;
        _workerStarter = workerStarter;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _databaseMigrator.EnsureReadyAsync(_scopeFactory, cancellationToken);
        await _workerStarter.StartAsync(_scopeFactory, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => _workerStarter.StopAsync(cancellationToken);
}
