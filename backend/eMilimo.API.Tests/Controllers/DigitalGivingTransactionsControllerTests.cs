using System.Text.Json;
using eMilimo.API.Controllers;
using eMilimo.API.Payments;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace eMilimo.API.Tests.Controllers;

public sealed class DigitalGivingTransactionsControllerTests
{
    [Fact]
    public async Task Summary_UsesSuccessfulPaymentsAndReportsUnreconciledCount()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Db.DigitalCollections.AddRange(
            fixture.Payment("DGP-1", PaymentTransactionStatuses.Succeeded, PaymentSettlementStatuses.Pending, 300m),
            fixture.Payment("DGP-2", PaymentTransactionStatuses.Failed, PaymentSettlementStatuses.Pending, 100m));
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Controller.Summary(null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"totalReceived\":300", json);
        Assert.Contains("\"successful\":1", json);
        Assert.Contains("\"failed\":1", json);
        Assert.Contains("\"unreconciled\":1", json);
    }

    [Fact]
    public async Task List_AppliesSearchAndStatusWithoutClientSideEvaluation()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Db.DigitalCollections.AddRange(
            fixture.Payment("DGP-ALPHA", PaymentTransactionStatuses.Pending, PaymentSettlementStatuses.Pending, 50m, "Mary Phiri"),
            fixture.Payment("DGP-BETA", PaymentTransactionStatuses.Succeeded, PaymentSettlementStatuses.Settled, 75m, "John Banda"));
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Controller.List("mary", PaymentTransactionStatuses.Pending, null, null, null, null, 1, 25, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("DGP-ALPHA", json);
        Assert.DoesNotContain("DGP-BETA", json);
        Assert.Contains("\"total\":1", json);
    }

    [Fact]
    public async Task Reconcile_RequiresSuccessfulPaymentAndWritesAuditEvent()
    {
        await using var fixture = await Fixture.CreateAsync();
        var payment = fixture.Payment("DGP-RECON", PaymentTransactionStatuses.Succeeded, PaymentSettlementStatuses.Settled, 90m);
        fixture.Db.DigitalCollections.Add(payment);
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Controller.Reconcile(payment.Id, new TransactionActionRequest("Bank statement matched"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        fixture.Db.ChangeTracker.Clear();
        var saved = await fixture.Db.DigitalCollections.SingleAsync(x => x.Id == payment.Id);
        Assert.Equal(PaymentSettlementStatuses.Reconciled, saved.SettlementStatus);
        Assert.True(await fixture.Db.DigitalCollectionEvents.AnyAsync(x => x.DigitalCollectionId == payment.Id && x.EventType == "PAYMENT_RECONCILED"));
    }

    [Fact]
    public async Task Reversal_Request_DoesNotChangeBalancesOrProviderFunds()
    {
        await using var fixture = await Fixture.CreateAsync();
        var payment = await fixture.PostedPaymentAsync("DGP-REV-REQUEST", 125m);

        var result = await fixture.Controller.RequestReversal(payment.Id, new TransactionActionRequest("Contributor payment must be corrected"), CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        var reversal = await fixture.Db.FinancialAdjustments.SingleAsync();
        Assert.Equal(FinancialAdjustmentStatuses.Requested, reversal.Status);
        Assert.Equal(FinancialAdjustmentTypes.AccountingReversal, reversal.AdjustmentType);
        Assert.Equal(125m, (await fixture.Db.FundBalances.SingleAsync()).OwnFundsBalance);
        Assert.False((await fixture.Db.FundTransactions.SingleAsync()).IsReversed);
    }

    [Fact]
    public async Task Reversal_MakerCannotApproveOwnRequest()
    {
        await using var fixture = await Fixture.CreateAsync();
        var payment = await fixture.PostedPaymentAsync("DGP-REV-SELF", 80m);
        await fixture.Controller.RequestReversal(payment.Id, new TransactionActionRequest("Duplicate payment"), CancellationToken.None);
        var reversal = await fixture.Db.FinancialAdjustments.SingleAsync();

        var result = await fixture.Controller.ApproveReversal(payment.Id, reversal.Id, new TransactionActionRequest(null), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("maker", JsonSerializer.Serialize(conflict.Value), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(80m, (await fixture.Db.FundBalances.SingleAsync()).OwnFundsBalance);
    }

    [Fact]
    public async Task Reversal_CheckerApproval_ReversesLedgerAndArchivesContributionAtomically()
    {
        await using var fixture = await Fixture.CreateAsync();
        var payment = await fixture.PostedPaymentAsync("DGP-REV-APPROVE", 210m);
        await fixture.Controller.RequestReversal(payment.Id, new TransactionActionRequest("Verified duplicate collection"), CancellationToken.None);
        var reversal = await fixture.Db.FinancialAdjustments.AsNoTracking().SingleAsync();

        var result = await fixture.CreateController(fixture.CheckerId)
            .ApproveReversal(payment.Id, reversal.Id, new TransactionActionRequest("Checker verified source records"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        fixture.Db.ChangeTracker.Clear();
        Assert.Equal(PaymentTransactionStatuses.Reversed, (await fixture.Db.DigitalCollections.SingleAsync(x => x.Id == payment.Id)).Status);
        Assert.Equal(0m, (await fixture.Db.FundBalances.SingleAsync()).OwnFundsBalance);
        Assert.True((await fixture.Db.Contributions.SingleAsync()).IsArchived);
        Assert.Equal(FinancialAdjustmentStatuses.Approved, (await fixture.Db.FinancialAdjustments.SingleAsync()).Status);
        var ledger = await fixture.Db.FundTransactions.OrderBy(x => x.CreatedDate).ToListAsync();
        Assert.Equal(2, ledger.Count);
        Assert.True(ledger[0].IsReversed);
        Assert.Equal(-210m, ledger[1].Amount);
        Assert.Equal("DIGITAL_GIVING_REVERSAL", ledger[1].ReferenceType);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext Db { get; }
        public DigitalGivingTransactionsController Controller { get; }
        private readonly Guid _methodId;
        private readonly Guid _sourceId;
        public Guid MakerId { get; }
        public Guid CheckerId { get; }

        private Fixture(SqliteConnection connection, AppDbContext db, Guid methodId, Guid sourceId, Guid makerId, Guid checkerId)
        {
            _connection = connection; Db = db; _methodId = methodId; _sourceId = sourceId; MakerId = makerId; CheckerId = checkerId;
            Controller = CreateController(MakerId);
        }

        public DigitalGivingTransactionsController CreateController(Guid actorId) => new(
                Db, new CurrentUser(actorId), new NullWebhookProcessor(),
                new PaymentProviderRegistry([]), new NullProviderStore(),
                new NullFinalizationService(),
                NullLogger<DigitalGivingTransactionsController>.Instance,
                new EphemeralDataProtectionProvider());

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection).UseSnakeCaseNamingConvention().Options);
            await db.Database.EnsureCreatedAsync();
            var methodId = Guid.NewGuid();
            var sourceId = Guid.NewGuid();
            var makerId = Guid.NewGuid();
            var checkerId = Guid.NewGuid();
            db.PaymentMethodDefinitions.Add(new PaymentMethodDefinition
            {
                Id = methodId, Code = "MOBILE_MONEY", Name = "Mobile Money", Category = "DIGITAL",
                CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1
            });
            db.ContributionSources.Add(new ContributionSource { Id = sourceId, Name = "Digital Giving", IsActive = true, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1 });
            db.Users.AddRange(User(makerId, "Maker"), User(checkerId, "Checker"));
            await db.SaveChangesAsync();
            return new Fixture(connection, db, methodId, sourceId, makerId, checkerId);
        }

        private static User User(Guid id, string firstName) => new()
        {
            Id = id, FirstName = firstName, LastName = "Steward", Email = $"{firstName.ToLowerInvariant()}@example.test",
            PhoneNumber = "0970000000", MembershipId = id.ToString("N"), Role = "Admin", Status = "ACTIVE",
            CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1
        };

        public DigitalCollection Payment(string reference, string status, string settlement, decimal amount, string? name = null) => new()
        {
            Id = Guid.NewGuid(), PaymentReference = reference, IdempotencyKey = $"idem-{Guid.NewGuid():N}",
            RequestFingerprint = Guid.NewGuid().ToString("N"), PublicStatusTokenHash = Guid.NewGuid().ToString("N"),
            ProtectedPublicStatusToken = "protected", GiverType = name is null ? DigitalGiverTypes.Anonymous : DigitalGiverTypes.Guest,
            ContributorNameSnapshot = name, PaymentMethodDefinitionId = _methodId, ContributionSourceId = _sourceId,
            Status = status, SettlementStatus = settlement, CurrencyCode = "ZMW", GrossAmount = amount,
            ProviderEnvironmentSnapshot = "PRODUCTION",
            NetSettlementAmount = status == PaymentTransactionStatuses.Succeeded ? amount : 0m,
            CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1
        };

        public async Task<DigitalCollection> PostedPaymentAsync(string reference, decimal amount)
        {
            var now = DateTime.UtcNow;
            var type = new ContributionType { Id = Guid.NewGuid(), Code = $"T-{Guid.NewGuid():N}", Name = "Tithe", IsActive = true, CreatedDate = now, UpdatedDate = now, Version = 1 };
            var contribution = new Contribution
            {
                Id = Guid.NewGuid(), ContributionNumber = $"CTR-{Guid.NewGuid():N}", ContributionTypeId = type.Id,
                SourceId = _sourceId, Amount = amount, ContributionDate = now, PaymentMethod = "MOBILE_MONEY",
                Status = "CONFIRMED", OriginType = "DIGITAL_GIVING", CurrencyCode = "ZMW", OriginalCurrencyAmount = amount,
                CreatedById = MakerId, UpdatedById = MakerId, CreatedDate = now, UpdatedDate = now, Version = 1
            };
            var payment = Payment(reference, PaymentTransactionStatuses.Succeeded, PaymentSettlementStatuses.Reconciled, amount);
            payment.Lines.Add(new DigitalCollectionItem
            {
                Id = Guid.NewGuid(), DigitalCollectionId = payment.Id, ContributionTypeId = type.Id,
                ContributionId = contribution.Id, Amount = amount, CreatedDate = now, UpdatedDate = now, Version = 1
            });
            Db.ContributionTypes.Add(type); Db.Contributions.Add(contribution); Db.DigitalCollections.Add(payment);
            Db.FundBalances.Add(new FundBalance { Id = Guid.NewGuid(), FiscalYear = now.Year, OwnFundsBalance = amount, CreatedDate = now, UpdatedDate = now, Version = 1 });
            Db.FundTransactions.Add(new FundTransaction
            {
                Id = Guid.NewGuid(), TransactionType = "ADD", FundType = "OWN_FUNDS", Amount = amount,
                PreviousBalance = 0m, NewBalance = amount, Description = "Digital contribution", Source = "Digital Giving",
                ReferenceType = "CONTRIBUTION", ReferenceId = contribution.Id, CreatedById = MakerId,
                CreatedDate = now, UpdatedDate = now, Version = 1
            });
            await Db.SaveChangesAsync();
            return payment;
        }

        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await _connection.DisposeAsync(); }
    }

    private sealed class CurrentUser(Guid id) : ICurrentUserService
    {
        public Guid? GetUserId() => id;
        public string? GetUserEmail() => "admin@example.test";
        public string? GetUserRole() => "Admin";
        public bool IsAuthenticated() => true;
    }

    private sealed class NullWebhookProcessor : IPaymentWebhookProcessor
    {
        public Task<PaymentWebhookProcessingResult> ProcessAsync(string providerKey, string rawBody, IReadOnlyDictionary<string, string> headers, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> SendReceiptNotificationAsync(string paymentReference, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class NullFinalizationService : IPaymentFinalizationService
    {
        public Task<PaymentFinalizationResult> ApplyVerifiedStatusAsync(
            Guid paymentTransactionId,
            ProviderVerifiedPayment verified,
            string source,
            string correlationId,
            Guid? actorUserId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Payment finalization is not used by these controller tests.");
    }

    private sealed class NullProviderStore : IPaymentProviderConfigurationStore
    {
        public Task<IReadOnlyList<PaymentProviderConfigurationView>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PaymentProviderConfigurationView>>([]);
        public Task<PaymentProviderConfigurationView> SaveAsync(PaymentProviderConfigurationDraft draft, Guid actorUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, string>> GetRuntimeSettingsAsync(Guid providerConfigurationId, bool requireEnabled = true, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
