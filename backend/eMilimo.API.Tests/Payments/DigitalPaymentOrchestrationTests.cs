using System.Security.Cryptography;
using System.Text;
using eMilimo.API.Payments;
using eMilimo.API.Platform;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Core.Enums;
using eMilimo.Core.Payments;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace eMilimo.API.Tests.Payments;

public sealed class DigitalPaymentOrchestrationTests
{
    [Fact]
    public async Task GuestIntent_SupportsMultipleContributionCategories()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var provider = new CapturePaymentProvider();
        var service = fixture.CreateIntentService(provider);

        var result = await service.CreateAsync(fixture.Request(
            DigitalGiverTypes.Guest,
            800m,
            [new(fixture.TitheTypeId, 500m), new(fixture.BuildingTypeId, 300m)],
            contributorName: "Mary Phiri"), "client-a");

        Assert.Equal(PaymentTransactionStatuses.Pending, result.Status);
        Assert.Equal(800m, result.GrossAmount);
        var payment = await fixture.Db.DigitalCollections.Include(x => x.Lines).SingleAsync();
        Assert.Equal("Mary Phiri", payment.ContributorNameSnapshot);
        Assert.Null(payment.MemberId);
        Assert.Equal(2, payment.Lines.Count);
        Assert.Equal(800m, payment.Lines.Sum(x => x.Amount));
        Assert.DoesNotContain("+260977000000", payment.ProtectedPayerData);
    }

    [Fact]
    public async Task AnonymousIntent_DoesNotExposeContributorIdentity()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var service = fixture.CreateIntentService(new CapturePaymentProvider());

        await service.CreateAsync(fixture.Request(
            DigitalGiverTypes.Anonymous,
            100m,
            [new(fixture.TitheTypeId, 100m)],
            contributorName: "Name must be ignored"), "client-a");

        var payment = await fixture.Db.DigitalCollections.SingleAsync();
        Assert.Null(payment.MemberId);
        Assert.Null(payment.ContributorNameSnapshot);
    }

    [Fact]
    public async Task RegisteredMemberIntent_ConsumesVerifiedChallengeAndLinksMember()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var token = "verified-member-token";
        var member = new Member
        {
            Id = Guid.NewGuid(), MembershipNumber = "MEM-100", FirstName = "John", LastName = "Zulu",
            PreferredName = "John Zulu", NormalizedSearchName = "JOHN ZULU", Status = MembershipStatuses.Active, IsActive = true
        };
        var challenge = fixture.VerifiedChallenge(member.Id, token, "client-a");
        fixture.Db.AddRange(member, challenge);
        await fixture.Db.SaveChangesAsync();
        var request = fixture.Request(DigitalGiverTypes.Member, 100m, [new(fixture.TitheTypeId, 100m)]) with
        {
            MemberVerificationChallengeId = challenge.Id,
            MemberVerificationToken = token
        };

        await fixture.CreateIntentService(new CapturePaymentProvider()).CreateAsync(request, "client-a");

        var payment = await fixture.Db.DigitalCollections.SingleAsync();
        Assert.Equal(member.Id, payment.MemberId);
        Assert.Equal("John Zulu", payment.ContributorNameSnapshot);
        Assert.NotNull((await fixture.Db.MemberVerificationChallenges.SingleAsync()).ConsumedAtUtc);
    }

    [Fact]
    public async Task DuplicateIntent_ReturnsOriginalAndDoesNotInitiateAnotherCharge()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var provider = new CapturePaymentProvider();
        var service = fixture.CreateIntentService(provider);
        var request = fixture.Request(DigitalGiverTypes.Guest, 100m, [new(fixture.TitheTypeId, 100m)], "Jane");

        var first = await service.CreateAsync(request, "client-a");
        var replay = await service.CreateAsync(request, "client-a");

        Assert.False(first.IsIdempotentReplay);
        Assert.True(replay.IsIdempotentReplay);
        Assert.Equal(first.PaymentReference, replay.PaymentReference);
        Assert.Equal(first.PublicStatusToken, replay.PublicStatusToken);
        Assert.Equal(1, provider.InitiationCount);
        Assert.Equal(1, await fixture.Db.DigitalCollections.CountAsync());
    }

    [Fact]
    public async Task FailedProviderInitiation_IsRecordedAndNotRoutedElsewhere()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var provider = new CapturePaymentProvider { ThrowOnInitiation = true };
        var service = fixture.CreateIntentService(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            fixture.Request(DigitalGiverTypes.Guest, 100m, [new(fixture.TitheTypeId, 100m)], "Jane"),
            "client-a"));

        var payment = await fixture.Db.DigitalCollections.SingleAsync();
        Assert.Equal(PaymentTransactionStatuses.Failed, payment.Status);
        Assert.Equal(1, provider.InitiationCount);
    }

    [Fact]
    public async Task VerifiedWebhook_PostsEveryLineAtomicallyAndDuplicateDoesNotPostAgain()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var provider = new CapturePaymentProvider();
        var intent = await fixture.CreateIntentService(provider).CreateAsync(fixture.Request(
            DigitalGiverTypes.Guest,
            800m,
            [new(fixture.TitheTypeId, 500m), new(fixture.BuildingTypeId, 300m)],
            "Mary Phiri"), "client-a");
        provider.WebhookResult = fixture.SuccessWebhook("event-1", 800m);
        var processor = fixture.CreateWebhookProcessor(provider, new ContributionPostingService(fixture.Db));

        var first = await processor.ProcessAsync("TEST_PROVIDER", "{}", new Dictionary<string, string>(), "corr-1");
        var duplicate = await processor.ProcessAsync("TEST_PROVIDER", "{}", new Dictionary<string, string>(), "corr-2");

        Assert.True(first.Accepted);
        Assert.False(first.Duplicate);
        Assert.True(duplicate.Duplicate);
        Assert.Equal(2, await fixture.Db.Contributions.CountAsync());
        Assert.All(await fixture.Db.Contributions.ToListAsync(), x => Assert.Equal(ContributionStatus.CONFIRMED.ToString(), x.Status));
        Assert.Equal(2, await fixture.Db.FundTransactions.CountAsync());
        Assert.Equal(800m, (await fixture.Db.FundBalances.ToListAsync()).Sum(x => x.OwnFundsBalance));
        Assert.Equal(PaymentTransactionStatuses.Succeeded, (await fixture.Db.DigitalCollections.SingleAsync()).Status);
        Assert.Equal(PaymentSettlementStatuses.Settled, (await fixture.Db.DigitalCollections.SingleAsync()).SettlementStatus);
        Assert.Equal(intent.PaymentReference, first.PaymentReference);
    }

    [Fact]
    public async Task VerifiedSandboxPayment_ExercisesCompleteFinancialWorkflow()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var provider = new CapturePaymentProvider();
        await fixture.CreateIntentService(provider).CreateAsync(fixture.Request(
            DigitalGiverTypes.Guest, 100m, [new(fixture.TitheTypeId, 100m)], "Sandbox Giver"), "client-a");
        var payment = await fixture.Db.DigitalCollections.SingleAsync();
        payment.ProviderEnvironmentSnapshot = "SANDBOX";
        await fixture.Db.SaveChangesAsync();
        provider.WebhookResult = fixture.SuccessWebhook("sandbox-event-1", 100m);

        var result = await fixture.CreateWebhookProcessor(provider, new ContributionPostingService(fixture.Db))
            .ProcessAsync("TEST_PROVIDER", "{}", new Dictionary<string, string>(), "sandbox-correlation");

        Assert.True(result.Accepted);
        fixture.Db.ChangeTracker.Clear();
        var saved = await fixture.Db.DigitalCollections.Include(x => x.Lines).SingleAsync();
        Assert.Equal(PaymentTransactionStatuses.Succeeded, saved.Status);
        Assert.Equal(PaymentSettlementStatuses.Settled, saved.SettlementStatus);
        Assert.All(saved.Lines, line => Assert.NotNull(line.ContributionId));
        Assert.Single(await fixture.Db.Contributions.ToListAsync());
        Assert.Single(await fixture.Db.FundTransactions.ToListAsync());
        Assert.Equal(100m, (await fixture.Db.FundBalances.ToListAsync()).Sum(x => x.OwnFundsBalance));
        Assert.True(await fixture.Db.DigitalCollectionEvents.AnyAsync(x => x.EventType == "SANDBOX_FULL_WORKFLOW_POSTED"));
    }

    [Fact]
    public async Task PublicAutomaticRefresh_QueriesProviderAndFinalizesSuccessfulPayment()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var provider = new CapturePaymentProvider();
        var intent = await fixture.CreateIntentService(provider).CreateAsync(fixture.Request(
            DigitalGiverTypes.Guest, 100m, [new(fixture.TitheTypeId, 100m)], "Automatic Refresh"), "client-a");
        provider.QueryResult = new PaymentStatusResult(
            PaymentTransactionStatuses.Succeeded, "provider-transaction-1", 0m, 100m,
            DateTime.UtcNow, "Completed", 100m, "ZMW");
        var finalization = new PaymentFinalizationService(
            fixture.Db,
            new SequenceReferenceService(() => 900001),
            new ContributionPostingService(fixture.Db),
            new TestDistributedLock(),
            NullLogger<PaymentFinalizationService>.Instance);
        var notifications = fixture.CreateWebhookProcessor(provider, new ContributionPostingService(fixture.Db));
        var refresh = new PublicPaymentStatusRefreshService(
            fixture.Db,
            new PaymentProviderRegistry([provider]),
            new StubProviderConfigurationStore(),
            finalization,
            notifications,
            new TestDistributedLock(),
            NullLogger<PublicPaymentStatusRefreshService>.Instance);

        await refresh.RefreshAsync(intent.PaymentReference, intent.PublicStatusToken, "public-refresh-test");

        fixture.Db.ChangeTracker.Clear();
        Assert.Equal(PaymentTransactionStatuses.Succeeded,
            (await fixture.Db.DigitalCollections.SingleAsync()).Status);
        Assert.Equal(PaymentSettlementStatuses.Settled,
            (await fixture.Db.DigitalCollections.SingleAsync()).SettlementStatus);
        Assert.Single(await fixture.Db.Contributions.ToListAsync());
        Assert.Single(await fixture.Db.FundTransactions.ToListAsync());
    }

    [Fact]
    public async Task PostingFailure_RollsBackAllContributionLines()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var provider = new CapturePaymentProvider();
        await fixture.CreateIntentService(provider).CreateAsync(fixture.Request(
            DigitalGiverTypes.Guest,
            800m,
            [new(fixture.TitheTypeId, 500m), new(fixture.BuildingTypeId, 300m)],
            "Mary Phiri"), "client-a");
        provider.WebhookResult = fixture.SuccessWebhook("event-fail", 800m);
        var processor = fixture.CreateWebhookProcessor(provider, new ThrowOnSecondPostingService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => processor.ProcessAsync(
            "TEST_PROVIDER", "{\"failure\":true}", new Dictionary<string, string>(), "corr-fail"));

        fixture.Db.ChangeTracker.Clear();
        Assert.Empty(await fixture.Db.Contributions.ToListAsync());
        Assert.Empty(await fixture.Db.FundTransactions.ToListAsync());
        Assert.All(await fixture.Db.DigitalCollectionItems.ToListAsync(), x => Assert.Null(x.ContributionId));
        Assert.Equal(PaymentTransactionStatuses.Pending, (await fixture.Db.DigitalCollections.SingleAsync()).Status);
        Assert.Equal("FAILED", (await fixture.Db.PaymentWebhookEvents.SingleAsync()).ProcessingStatus);
    }

    [Fact]
    public async Task TerminalFailedPayment_CannotTransitionToSucceededAutomatically()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var provider = new CapturePaymentProvider();
        await fixture.CreateIntentService(provider).CreateAsync(
            fixture.Request(DigitalGiverTypes.Guest, 100m, [new(fixture.TitheTypeId, 100m)], "Jane"), "client-a");
        var payment = await fixture.Db.DigitalCollections.SingleAsync();
        payment.Status = PaymentTransactionStatuses.Failed;
        await fixture.Db.SaveChangesAsync();
        provider.WebhookResult = fixture.SuccessWebhook("event-transition", 100m);

        await Assert.ThrowsAsync<PaymentConflictException>(() => fixture
            .CreateWebhookProcessor(provider, new ContributionPostingService(fixture.Db))
            .ProcessAsync("TEST_PROVIDER", "{}", new Dictionary<string, string>(), "corr-transition"));

        fixture.Db.ChangeTracker.Clear();
        Assert.Empty(await fixture.Db.Contributions.ToListAsync());
        Assert.Equal(PaymentTransactionStatuses.Failed, (await fixture.Db.DigitalCollections.SingleAsync()).Status);
    }

    [Fact]
    public async Task InvalidWebhookSignature_IsRejectedWithoutFinancialPosting()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var provider = new CapturePaymentProvider();
        await fixture.CreateIntentService(provider).CreateAsync(
            fixture.Request(DigitalGiverTypes.Guest, 100m, [new(fixture.TitheTypeId, 100m)], "Jane"), "client-a");
        provider.WebhookResult = new PaymentWebhookResult(
            false, "forged-event", "provider-transaction-1", PaymentTransactionStatuses.Succeeded,
            100m, "ZMW", 0m, 100m, DateTime.UtcNow, "Invalid signature");

        var result = await fixture.CreateWebhookProcessor(provider, new ContributionPostingService(fixture.Db))
            .ProcessAsync("TEST_PROVIDER", "{\"forged\":true}", new Dictionary<string, string>(), "corr-forged");

        Assert.False(result.Accepted);
        Assert.Equal("REJECTED", result.ProcessingStatus);
        Assert.Empty(await fixture.Db.Contributions.ToListAsync());
        Assert.Equal(PaymentTransactionStatuses.Pending, (await fixture.Db.DigitalCollections.SingleAsync()).Status);
    }

    [Fact]
    public async Task MismatchedProviderAmount_IsRejectedAndRequiresReconciliation()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var provider = new CapturePaymentProvider();
        await fixture.CreateIntentService(provider).CreateAsync(
            fixture.Request(DigitalGiverTypes.Guest, 100m, [new(fixture.TitheTypeId, 100m)], "Jane"), "client-a");
        provider.WebhookResult = fixture.SuccessWebhook("wrong-amount", 99m);

        await Assert.ThrowsAsync<PaymentConflictException>(() => fixture
            .CreateWebhookProcessor(provider, new ContributionPostingService(fixture.Db))
            .ProcessAsync("TEST_PROVIDER", "{\"amount\":99}", new Dictionary<string, string>(), "corr-amount"));

        fixture.Db.ChangeTracker.Clear();
        Assert.Empty(await fixture.Db.Contributions.ToListAsync());
        Assert.Equal(PaymentTransactionStatuses.Pending, (await fixture.Db.DigitalCollections.SingleAsync()).Status);
        Assert.Equal("FAILED", (await fixture.Db.PaymentWebhookEvents.SingleAsync()).ProcessingStatus);
    }

    [Fact]
    public async Task CompetingPaymentUpdates_RejectTheStaleWriter()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        await fixture.CreateIntentService(new CapturePaymentProvider()).CreateAsync(
            fixture.Request(DigitalGiverTypes.Guest, 100m, [new(fixture.TitheTypeId, 100m)], "Jane"), "client-a");
        await using var firstContext = fixture.CreatePeerContext();
        await using var secondContext = fixture.CreatePeerContext();
        var first = await firstContext.DigitalCollections.SingleAsync();
        var second = await secondContext.DigitalCollections.SingleAsync();
        first.Status = PaymentTransactionStatuses.Processing;
        first.Version++;
        second.Status = PaymentTransactionStatuses.Failed;
        second.Version++;

        await firstContext.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }

    private sealed class PaymentFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly EphemeralDataProtectionProvider _dataProtection = new();
        private int _referenceSequence;

        private PaymentFixture(SqliteConnection connection, AppDbContext db) { _connection = connection; Db = db; }
        public AppDbContext Db { get; }
        public Guid TitheTypeId { get; private init; }
        public Guid BuildingTypeId { get; private init; }
        public Guid PaymentMethodId { get; private init; }
        public Guid ProviderConfigurationId { get; private init; }

        public static async Task<PaymentFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection).UseSnakeCaseNamingConvention().Options;
            var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();
            var fixture = new PaymentFixture(connection, db)
            {
                TitheTypeId = Guid.NewGuid(), BuildingTypeId = Guid.NewGuid(),
                PaymentMethodId = Guid.NewGuid(), ProviderConfigurationId = Guid.NewGuid()
            };
            var source = new ContributionSource { Id = Guid.NewGuid(), Name = "Digital Giving", IsActive = true };
            var tithe = new ContributionType
            {
                Id = fixture.TitheTypeId, Code = "TITHE", Name = "Tithe", IsActive = true,
                IsEnabledForDigitalGiving = true, AllowSectionAllocation = true
            };
            var building = new ContributionType
            {
                Id = fixture.BuildingTypeId, Code = "BUILDING", Name = "Building Fund", IsActive = true,
                IsEnabledForDigitalGiving = true, AllowSectionAllocation = true
            };
            var section1 = new Section { Id = Guid.NewGuid(), SectionNumber = 1, SectionName = "General", IsActive = true };
            var section2 = new Section { Id = Guid.NewGuid(), SectionNumber = 2, SectionName = "Building", IsActive = true };
            var method = new PaymentMethodDefinition { Id = fixture.PaymentMethodId, Code = "MOBILE_MONEY", Name = "Mobile Money", Category = "DIGITAL", IsActive = true };
            var provider = new PaymentProviderConfiguration
            {
                Id = fixture.ProviderConfigurationId, ProviderKey = "TEST_PROVIDER", DisplayName = "Test Provider",
                ProviderKind = PaymentProviderKinds.Sandbox, Environment = "PRODUCTION", IsEnabled = true, ProtectedConfiguration = "test"
            };
            var config = new DigitalGivingConfiguration
            {
                Id = Guid.NewGuid(), IsEnabled = true, PublicSlug = "trinity-giving", CurrencyCode = "ZMW",
                MinimumAmount = 1m, ContributionSourceId = source.Id
            };
            config.ContributionTypes.Add(new DigitalGivingTypeSetting
            {
                Id = Guid.NewGuid(), ContributionTypeId = tithe.Id, IsPubliclyAvailable = true, DisplayOrder = 1, SectionId = section1.Id
            });
            config.ContributionTypes.Add(new DigitalGivingTypeSetting
            {
                Id = Guid.NewGuid(), ContributionTypeId = building.Id, IsPubliclyAvailable = true, DisplayOrder = 2, SectionId = section2.Id
            });
            db.AddRange(source, tithe, building, section1, section2, method, provider, config);
            await db.SaveChangesAsync();
            return fixture;
        }

        public CreateDigitalPaymentIntentRequest Request(
            string giverType,
            decimal total,
            IReadOnlyList<DigitalGivingLineRequest> lines,
            string? contributorName = null) => new(
                "trinity-giving", giverType, PaymentMethodId, total, "ZMW", "0977000000",
                contributorName, "giver@example.test", null, null,
                $"idempotency-{Guid.NewGuid():N}", lines);

        public MemberVerificationChallenge VerifiedChallenge(Guid memberId, string token, string fingerprint) => new()
        {
            Id = Guid.NewGuid(), LookupHash = Hash("MEM-100"), MemberId = memberId,
            ClientFingerprintHash = Hash(fingerprint), OtpSalt = new byte[16], OtpHash = new byte[32],
            IsVerified = true, VerifiedAtUtc = DateTime.UtcNow, VerificationTokenHash = Hash(token),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10), MaximumAttempts = 5
        };

        public PaymentWebhookResult SuccessWebhook(string eventKey, decimal amount) => new(
            true, eventKey, "provider-transaction-1", PaymentTransactionStatuses.Succeeded,
            amount, "ZMW", 8m, amount - 8m, DateTime.UtcNow, null);

        public AppDbContext CreatePeerContext() => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).UseSnakeCaseNamingConvention().Options);

        public DigitalPaymentIntentService CreateIntentService(CapturePaymentProvider provider)
        {
            var registry = new PaymentProviderRegistry([provider]);
            return new DigitalPaymentIntentService(
                Db,
                new StubRoutingService(ProviderConfigurationId, PaymentMethodId),
                registry,
                new StubProviderConfigurationStore(),
                new SequenceReferenceService(() => Interlocked.Increment(ref _referenceSequence)),
                new TestHostEnvironment(Environments.Development),
                new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DigitalGiving:PublicApiBaseUrl"] = "https://giving.example.test"
                }).Build(),
                _dataProtection,
                new ContributionDestinationValidationService(Db),
                NullLogger<DigitalPaymentIntentService>.Instance);
        }

        public PaymentWebhookProcessor CreateWebhookProcessor(CapturePaymentProvider provider, IContributionPostingService posting) => new(
            Db,
            new PaymentProviderRegistry([provider]),
            new StubProviderConfigurationStore(),
            _dataProtection,
            new NullEmailService(),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:Url"] = "https://giving.example.test"
            }).Build(),
            new TestHostEnvironment(Environments.Development),
                NullLogger<PaymentWebhookProcessor>.Instance,
                new PaymentFinalizationService(
                    Db,
                    new SequenceReferenceService(() => Interlocked.Increment(ref _referenceSequence)),
                    posting,
                    new TestDistributedLock(),
                    NullLogger<PaymentFinalizationService>.Instance));

        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await _connection.DisposeAsync(); }

        private static string Hash(string value) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private sealed class TestDistributedLock : IDistributedLock
    {
        public Task<IAsyncDisposable?> TryAcquireAsync(string resource, CancellationToken cancellationToken = default) =>
            Task.FromResult<IAsyncDisposable?>(new Lease());

        private sealed class Lease : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class NullEmailService : IEmailService
    {
        public Task SendPasswordResetAsync(string toEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendTemporaryPasswordAsync(string toEmail, string recipientName, string temporaryPassword, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendAccountActivatedAsync(string toEmail, string recipientName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendAccountDeactivatedAsync(string toEmail, string recipientName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendRequisitionStatusChangedAsync(string toEmail, string recipientName, string requisitionNumber, string newStatus, string? note, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendContributionStatusChangedAsync(string toEmail, string recipientName, string contributionNumber, string newStatus, string? note, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendAssetRequestStatusChangedAsync(string toEmail, string recipientName, string requestNumber, string assetName, int quantity, string newStatus, DateTime expectedOutAtUtc, DateTime expectedReturnAtUtc, bool paymentRequired, decimal? paymentAmount, string? note, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendApprovalActionRequiredAsync(string toEmail, string recipientName, string processName, string recordNumber, string summary, string approvalUrl, string[] actions, IReadOnlyCollection<string>? ccEmails = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendTemplateTestAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendTestEmailAsync(string toEmail, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CapturePaymentProvider : IPaymentProvider
    {
        public int InitiationCount { get; private set; }
        public bool ThrowOnInitiation { get; init; }
        public PaymentWebhookResult? WebhookResult { get; set; }
        public PaymentStatusResult? QueryResult { get; set; }
        public PaymentProviderDescriptor Descriptor { get; } = new(
            "TEST_PROVIDER", "Test Provider", PaymentProviderKinds.Sandbox, true, false, ["MOBILE_MONEY"]);

        public Task<PaymentInitiationResult> InitiateAsync(PaymentInitiationRequest request, CancellationToken cancellationToken = default)
        {
            InitiationCount++;
            if (ThrowOnInitiation) throw new InvalidOperationException("Provider unavailable");
            return Task.FromResult(new PaymentInitiationResult(
                PaymentTransactionStatuses.Pending, "provider-transaction-1", "Prompt sent", DateTime.UtcNow.AddMinutes(5)));
        }

        public Task<PaymentStatusResult> QueryStatusAsync(string providerTransactionId, IReadOnlyDictionary<string, string> providerSettings, CancellationToken cancellationToken = default) =>
            Task.FromResult(QueryResult ?? new PaymentStatusResult(PaymentTransactionStatuses.Pending, providerTransactionId, null, null, null, null));

        public Task<PaymentWebhookResult> VerifyAndParseWebhookAsync(PaymentWebhookRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(WebhookResult ?? throw new InvalidOperationException("Webhook result not configured."));
    }

    private sealed class StubRoutingService(Guid providerConfigurationId, Guid paymentMethodId) : IPaymentRoutingService
    {
        public Task<PaymentRouteSelection> SelectInitialRouteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == paymentMethodId
                ? new PaymentRouteSelection(providerConfigurationId, "TEST_PROVIDER", "TEST_MOMO", 1)
                : throw new KeyNotFoundException());
        public Task<IReadOnlyList<PaymentMethodAvailability>> GetAvailableMethodsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaymentMethodAvailability>>([new(paymentMethodId, "MOBILE_MONEY", "Mobile Money", "DIGITAL")]);
    }

    private sealed class StubProviderConfigurationStore : IPaymentProviderConfigurationStore
    {
        public Task<IReadOnlyList<PaymentProviderConfigurationView>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaymentProviderConfigurationView>>([]);
        public Task<PaymentProviderConfigurationView> SaveAsync(PaymentProviderConfigurationDraft draft, Guid actorUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, string>> GetRuntimeSettingsAsync(Guid id, bool requireEnabled = true, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string> { ["_Environment"] = "PRODUCTION" });
    }

    private sealed class SequenceReferenceService(Func<int> next) : IReferenceNumberService
    {
        public Task<string> NextAsync(string referenceType, DateOnly? date = null, CancellationToken cancellationToken = default) =>
            Task.FromResult($"{(referenceType == "CONTRIBUTION" ? "CON" : "DGP")}{next():D6}20261024");
    }

    private sealed class ThrowOnSecondPostingService : IContributionPostingService
    {
        private int _count;
        public Task<ContributionPostingResult> PostAsync(ContributionPostingRequest request, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _count) == 2) throw new InvalidOperationException("Injected posting failure");
            return Task.FromResult(new ContributionPostingResult(request.ContributionId, false, 1, 0m));
        }
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "eMilimo.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
