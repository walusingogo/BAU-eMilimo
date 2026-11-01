using eMilimo.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace eMilimo.API.Tests.Services;

public class OutboxStartupTests
{
    [Fact]
    public async Task DelayedOutboxWorkerHostedService_ExecutesMigrationBeforeWorkerStarts()
    {
        var tracker = new StartupOrderTracker();
        var hostedService = new DelayedOutboxWorkerHostedService(
            new TestScopeFactory(),
            new TrackingDatabaseMigrator(tracker),
            new TrackingWorkerStarter(tracker));

        await hostedService.StartAsync(CancellationToken.None);

        Assert.True(tracker.MigrationRan);
        Assert.True(tracker.WorkerStarted);
        Assert.True(tracker.MigrationRanBeforeWorkerStart);
    }

    private sealed class StartupOrderTracker
    {
        public bool MigrationRan { get; private set; }
        public bool WorkerStarted { get; private set; }
        public bool MigrationRanBeforeWorkerStart { get; private set; }

        public void RecordMigration() => MigrationRan = true;

        public void RecordWorkerStart()
        {
            WorkerStarted = true;
            if (MigrationRan)
            {
                MigrationRanBeforeWorkerStart = true;
            }
        }
    }

    private sealed class TrackingDatabaseMigrator : IOutboxDatabaseMigrator
    {
        private readonly StartupOrderTracker _tracker;

        public TrackingDatabaseMigrator(StartupOrderTracker tracker)
            => _tracker = tracker;

        public Task EnsureReadyAsync(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            _tracker.RecordMigration();
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingWorkerStarter : IOutboxWorkerStarter
    {
        private readonly StartupOrderTracker _tracker;

        public TrackingWorkerStarter(StartupOrderTracker tracker)
            => _tracker = tracker;

        public Task StartAsync(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            _tracker.RecordWorkerStart();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new TestScope();

        private sealed class TestScope : IServiceScope
        {
            public IServiceProvider ServiceProvider => new ServiceCollection().BuildServiceProvider();
            public void Dispose() { }
        }
    }
}
