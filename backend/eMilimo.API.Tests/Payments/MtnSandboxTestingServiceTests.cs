using System.Reflection;
using System.Text.Json;
using eMilimo.API.Authorization;
using eMilimo.API.Controllers;
using eMilimo.API.Payments;
using eMilimo.Core.Payments;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eMilimo.API.Tests.Payments;

public sealed class MtnSandboxTestingServiceTests
{
    [Fact]
    public async Task Catalog_seeds_documented_and_inferred_scenarios_and_samples()
    {
        await using var fixture = await Fixture.CreateAsync();

        await fixture.Service.CatalogAsync(CancellationToken.None);

        var scenarios = await fixture.Db.MtnSandboxScenarios.OrderBy(x => x.Msisdn).ToListAsync();
        Assert.Equal(19, scenarios.Count);
        Assert.Equal(15, scenarios.Count(x => x.SourceClassification == "DOCUMENTED"));
        Assert.Equal(4, scenarios.Count(x => x.SourceClassification == "INFERRED"));
        Assert.All(scenarios, x => Assert.True(x.IsSystemDefault));
        Assert.Equal(4, scenarios.Sum(x =>
            (JsonSerializer.Deserialize<MtnSandboxSamplePreset[]>(x.SamplePresetsJson) ?? []).Length));
        Assert.Contains(scenarios, x => x.Msisdn == "46733123450" && x.Name == "PayerFailed");
        Assert.Contains(scenarios, x => x.Msisdn == "46733123465" && x.ExpectedTransactionStatus == PaymentTransactionStatuses.Succeeded);
    }

    [Fact]
    public async Task Custom_scenario_can_be_saved_duplicated_and_persists()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Service.CatalogAsync(CancellationToken.None);
        var actor = Guid.NewGuid();

        await fixture.Service.SaveScenarioAsync(new MtnSandboxScenarioDraft(
            null, "CUSTOM_NETWORK_DELAY", "Custom network delay", "Local repeatable sandbox expectation.",
            "46733123454", PaymentTransactionStatuses.Pending, 202, null, 2m, "EUR", "CUSTOM-SBX",
            "Custom payer message", "Custom payee note", "DELAYED", 5, 3, true, "CUSTOM", null), actor, CancellationToken.None);

        var saved = await fixture.Db.MtnSandboxScenarios.SingleAsync(x => x.ScenarioCode == "CUSTOM_NETWORK_DELAY");
        await fixture.Service.DuplicateScenarioAsync(saved.Id, actor, CancellationToken.None);
        Assert.Equal(2, await fixture.Db.MtnSandboxScenarios.CountAsync(x => !x.IsSystemDefault));
        Assert.Contains(await fixture.Db.MtnSandboxScenarios.Where(x => !x.IsSystemDefault).ToListAsync(), x => !x.IsActive && x.SourceClassification == "CUSTOM");
    }

    [Fact]
    public async Task Default_scenario_cannot_be_deleted()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Service.CatalogAsync(CancellationToken.None);
        var item = await fixture.Db.MtnSandboxScenarios.FirstAsync(x => x.IsSystemDefault);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.DeleteScenarioAsync(item.Id, CancellationToken.None));

        Assert.Contains("not deleted", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Developer_controller_requires_dedicated_permission()
    {
        var attribute = typeof(PaymentDeveloperController).GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(attribute);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private Fixture(SqliteConnection connection, AppDbContext db)
        {
            this.connection = connection;
            Db = db;
            Service = new MtnSandboxTestingService(db, new PaymentProviderRegistry([]), new NullConfigurationStore(), new HttpContextAccessor());
        }

        public AppDbContext Db { get; }
        public MtnSandboxTestingService Service { get; }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection).UseSnakeCaseNamingConvention().Options);
            await db.Database.EnsureCreatedAsync();
            return new Fixture(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class NullConfigurationStore : IPaymentProviderConfigurationStore
    {
        public Task<IReadOnlyList<PaymentProviderConfigurationView>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaymentProviderConfigurationView>>([]);
        public Task<PaymentProviderConfigurationView> SaveAsync(PaymentProviderConfigurationDraft draft, Guid actorUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, string>> GetRuntimeSettingsAsync(Guid paymentProviderConfigurationId, bool requireEnabled = true, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
