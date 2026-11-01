using eMilimo.API.Payments;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text;
using Xunit;

namespace eMilimo.API.Tests.Payments;

public sealed class PaymentFoundationTests
{
    [Fact]
    public async Task ConfigurationStore_ProtectsSecretsAndReturnsOnlyConfiguredKeys()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var registry = new PaymentProviderRegistry([new DevelopmentSandboxPaymentProvider()]);
        var store = new PaymentProviderConfigurationStore(
            fixture.Db,
            registry,
            new TestHostEnvironment(Environments.Development),
            new EphemeralDataProtectionProvider(),
            EndpointPolicy());
        var actorId = Guid.NewGuid();

        var saved = await store.SaveAsync(new PaymentProviderConfigurationDraft(
            null,
            "development-sandbox",
            "Development Sandbox",
            PaymentProviderKinds.Sandbox,
            "SANDBOX",
            IsEnabled: true,
            new Dictionary<string, string>
            {
                ["WebhookSecret"] = "never-store-me-in-plaintext",
                ["MerchantId"] = "test-merchant"
            }), actorId);

        var persisted = await fixture.Db.PaymentProviderConfigurations.SingleAsync();
        Assert.DoesNotContain("never-store-me-in-plaintext", persisted.ProtectedConfiguration);
        Assert.DoesNotContain("test-merchant", persisted.ProtectedConfiguration);
        Assert.Equal(["MerchantId", "WebhookSecret"], saved.ConfiguredSettingKeys);
        Assert.Empty(saved.VisibleSettings);

        var runtime = await store.GetRuntimeSettingsAsync(saved.Id);
        Assert.Equal("never-store-me-in-plaintext", runtime["WebhookSecret"]);
        Assert.Equal("test-merchant", runtime["MerchantId"]);
    }

    [Fact]
    public async Task ConfigurationStore_RejectsNonProductionProviderInProduction()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var store = new PaymentProviderConfigurationStore(
            fixture.Db,
            new PaymentProviderRegistry([new DevelopmentSandboxPaymentProvider()]),
            new TestHostEnvironment(Environments.Production),
            new EphemeralDataProtectionProvider(),
            EndpointPolicy());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(
            new PaymentProviderConfigurationDraft(
                null,
                "DEVELOPMENT_SANDBOX",
                "Development Sandbox",
                PaymentProviderKinds.Sandbox,
                "SANDBOX",
                true,
                new Dictionary<string, string>()),
            Guid.NewGuid()));

        Assert.Contains("not approved for production", error.Message);
        Assert.Empty(await fixture.Db.PaymentProviderConfigurations.ToListAsync());
    }

    [Fact]
    public async Task Routing_SelectsFirstActiveImplementedProvider()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var paymentMethod = new PaymentMethodDefinition
        {
            Id = Guid.NewGuid(),
            Code = "MOBILE_MONEY",
            Name = "Mobile Money",
            Category = "DIGITAL",
            IsActive = true
        };
        var unavailableConfiguration = ProviderConfiguration("AIRTEL_MONEY_DIRECT", "Airtel", true);
        var sandboxConfiguration = ProviderConfiguration("DEVELOPMENT_SANDBOX", "Sandbox", true);
        fixture.Db.AddRange(paymentMethod, unavailableConfiguration, sandboxConfiguration);
        fixture.Db.PaymentProviderRoutes.AddRange(
            Route(paymentMethod.Id, unavailableConfiguration.Id, 10, "AIRTEL_ZM"),
            Route(paymentMethod.Id, sandboxConfiguration.Id, 20, "SANDBOX_MOMO"));
        await fixture.Db.SaveChangesAsync();

        var router = new PaymentRoutingService(
            fixture.Db,
            new PaymentProviderRegistry([
                new AirtelMoneyPaymentProvider(),
                new DevelopmentSandboxPaymentProvider()
            ]),
            new TestHostEnvironment(Environments.Development));

        var selection = await router.SelectInitialRouteAsync(paymentMethod.Id);

        Assert.Equal(sandboxConfiguration.Id, selection.ProviderConfigurationId);
        Assert.Equal("DEVELOPMENT_SANDBOX", selection.ProviderKey);
        Assert.Equal(20, selection.Priority);
    }

    [Fact]
    public async Task SandboxProvider_NeverSelfCompletesPayment()
    {
        var provider = new DevelopmentSandboxPaymentProvider();

        var initiated = await provider.InitiateAsync(new PaymentInitiationRequest(
            Guid.NewGuid(),
            "DG-2026-000001",
            "MOBILE_MONEY",
            800m,
            "ZMW",
            "+260970000000",
            new Uri("https://example.test/webhooks/sandbox"),
            "idempotency-key",
            new Dictionary<string, string>()));
        var queried = await provider.QueryStatusAsync(
            initiated.ProviderTransactionId!,
            new Dictionary<string, string>());

        Assert.Equal(PaymentTransactionStatuses.Pending, initiated.Status);
        Assert.Equal(PaymentTransactionStatuses.Pending, queried.Status);
        Assert.Contains("No real funds", initiated.ProviderMessage);
    }

    [Fact]
    public async Task MtnProvider_InitiatesAndAuthenticatesCallbackByStatusQuery()
    {
        var requests = new List<HttpRequestMessage>();
        var factory = new StaticHttpClientFactory(new RecordingHandler(async request =>
        {
            requests.Add(await CloneAsync(request));
            if (request.RequestUri!.AbsolutePath.EndsWith("/collection/token/", StringComparison.Ordinal))
                return Json(HttpStatusCode.OK, "{\"access_token\":\"test-token\",\"expires_in\":3600}");
            if (request.Method == HttpMethod.Post)
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            return Json(HttpStatusCode.OK,
                "{\"amount\":\"10.00\",\"currency\":\"EUR\",\"financialTransactionId\":\"financial-1\",\"status\":\"SUCCESSFUL\"}");
        }));
        var provider = new MtnMomoPaymentProvider(
            factory,
            NullLogger<MtnMomoPaymentProvider>.Instance,
            EndpointPolicy(),
            new NullExchangeWriter(),
            new HttpContextAccessor());
        var transactionId = Guid.NewGuid();
        var settings = new Dictionary<string, string>
        {
            ["ApiBaseUrl"] = "https://sandbox.momodeveloper.mtn.com",
            ["SubscriptionKey"] = "subscription-secret",
            ["ApiUser"] = Guid.NewGuid().ToString(),
            ["ApiKey"] = "api-secret",
            ["TargetEnvironment"] = "sandbox"
        };

        var initiated = await provider.InitiateAsync(new PaymentInitiationRequest(
            transactionId, "DG-TEST", "MTN_MOMO", 10m, "EUR", "+260970000000",
            new Uri("https://giving.example.test/api/public/digital-giving/webhooks/MTN_MOMO_DIRECT"),
            "test-idempotency-key", settings));
        var callback = await provider.VerifyAndParseWebhookAsync(new PaymentWebhookRequest(
            $"{{\"externalId\":\"{transactionId:D}\",\"amount\":\"10.00\",\"currency\":\"EUR\",\"status\":\"SUCCESSFUL\"}}",
            new Dictionary<string, string>(), settings));

        Assert.Equal(PaymentTransactionStatuses.Pending, initiated.Status);
        Assert.Equal(transactionId.ToString("D"), initiated.ProviderTransactionId);
        Assert.True(callback.IsAuthentic);
        Assert.Equal(PaymentTransactionStatuses.Succeeded, callback.Status);
        var paymentRequest = Assert.Single(requests.Where(x => x.Method == HttpMethod.Post && x.RequestUri!.AbsolutePath.EndsWith("requesttopay")));
        Assert.Equal(transactionId.ToString("D"), paymentRequest.Headers.GetValues("X-Reference-Id").Single());
        Assert.Equal("sandbox", paymentRequest.Headers.GetValues("X-Target-Environment").Single());
        Assert.DoesNotContain("subscription-secret", await paymentRequest.Content!.ReadAsStringAsync());
        var tokenRequest = Assert.Single(requests.Where(x => x.RequestUri!.AbsolutePath.EndsWith("/collection/token/", StringComparison.Ordinal)));
        Assert.Null(tokenRequest.Content);
    }

    [Fact]
    public void EndpointPolicy_RejectsLocalAndUnapprovedHosts()
    {
        var policy = EndpointPolicy();

        Assert.Throws<InvalidOperationException>(() => policy.ValidateBaseUri("MTN_MOMO_DIRECT", "SANDBOX", "https://127.0.0.1"));
        Assert.Throws<InvalidOperationException>(() => policy.ValidateBaseUri("MTN_MOMO_DIRECT", "SANDBOX", "https://evil.example"));
        Assert.Throws<InvalidOperationException>(() => policy.ValidateRelativePath("//evil.example/requesttopay", "Request to Pay endpoint"));
        Assert.Throws<InvalidOperationException>(() => policy.ValidateCallback(
            new Uri("https://other.example/api/webhook"), "giving.example", "PRODUCTION"));
    }

    [Fact]
    public void DiagnosticSanitizer_RedactsCredentialsAndMasksPayerNumber()
    {
        var headers = PaymentDiagnosticSanitizer.Headers(new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer should-never-persist",
            ["Ocp-Apim-Subscription-Key"] = "subscription-secret",
            ["X-Correlation-Id"] = "safe-correlation"
        });
        var payload = PaymentDiagnosticSanitizer.Payload("{\"access_token\":\"secret\",\"payer\":{\"partyId\":\"260977123456\"}}");

        Assert.DoesNotContain("should-never-persist", headers);
        Assert.DoesNotContain("subscription-secret", headers);
        Assert.Contains("safe-correlation", headers);
        Assert.DoesNotContain("secret", payload!);
        Assert.DoesNotContain("260977123456", payload!);
        Assert.Contains("3456", payload!);
    }

    [Fact]
    public async Task MtnProvider_AmbiguousInitiationFailureIsNotClassifiedAsDefinitiveFailure()
    {
        var factory = new StaticHttpClientFactory(new RecordingHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/collection/token/", StringComparison.Ordinal))
                return Task.FromResult(Json(HttpStatusCode.OK, "{\"access_token\":\"test-token\",\"expires_in\":3600}"));
            throw new HttpRequestException("simulated network interruption");
        }));
        var provider = NewMtnProvider(factory);
        var settings = SandboxSettings();

        var error = await Assert.ThrowsAsync<PaymentProviderException>(() => provider.InitiateAsync(new PaymentInitiationRequest(
            Guid.NewGuid(), "DG-AMBIGUOUS", "MTN_MOMO", 10m, "EUR", "46733123450",
            new Uri("https://giving.example.test/webhook"), "key", settings)));

        Assert.True(error.IsAmbiguous);
        Assert.Equal("NETWORK", error.ErrorCategory);
    }

    [Fact]
    public async Task MtnProvider_NormalizesZambiaMsisdnAndValidatesMessagesBeforeCallingProvider()
    {
        var requests = new List<HttpRequestMessage>();
        var factory = new StaticHttpClientFactory(new RecordingHandler(async request =>
        {
            requests.Add(await CloneAsync(request));
            return request.RequestUri!.AbsolutePath.EndsWith("/collection/token/", StringComparison.Ordinal)
                ? Json(HttpStatusCode.OK, "{\"access_token\":\"test-token\",\"expires_in\":3600}")
                : new HttpResponseMessage(HttpStatusCode.Accepted);
        }));
        var provider = NewMtnProvider(factory);
        var settings = new Dictionary<string, string>
        {
            ["_Environment"] = "PRODUCTION",
            ["ApiBaseUrl"] = "https://proxy.momoapi.mtn.com",
            ["SubscriptionKey"] = "subscription-secret",
            ["ApiUser"] = Guid.NewGuid().ToString(),
            ["ApiKey"] = "api-secret",
            ["TargetEnvironment"] = "mtnzambia",
            ["SupportedCurrencies"] = "ZMW",
            ["MsisdnCountryCode"] = "260",
            ["MsisdnNationalLength"] = "9",
            ["MsisdnAllowedPrefixes"] = "97"
        };

        await provider.InitiateAsync(new PaymentInitiationRequest(Guid.NewGuid(), "DG-ZM", "MTN_MOMO", 25m, "ZMW", "0977123456",
            new Uri("https://giving.example.test/webhook"), "key", settings));
        var request = Assert.Single(requests.Where(x => x.Method == HttpMethod.Post && x.Content is not null));
        Assert.Contains("260977123456", await request.Content!.ReadAsStringAsync());

        var tooLong = new string('x', 161);
        var before = requests.Count;
        settings["PayerMessage"] = tooLong;
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.InitiateAsync(new PaymentInitiationRequest(Guid.NewGuid(), "DG-LONG", "MTN_MOMO", 25m, "ZMW", "0977123456",
            new Uri("https://giving.example.test/webhook"), "key-2", settings)));
        Assert.Equal(before, requests.Count);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private static PaymentProviderEndpointPolicy EndpointPolicy() =>
        new(new ConfigurationBuilder().Build());

    private static MtnMomoPaymentProvider NewMtnProvider(IHttpClientFactory factory) => new(
        factory,
        NullLogger<MtnMomoPaymentProvider>.Instance,
        EndpointPolicy(),
        new NullExchangeWriter(),
        new HttpContextAccessor());

    private static Dictionary<string, string> SandboxSettings() => new()
    {
        ["ApiBaseUrl"] = "https://sandbox.momodeveloper.mtn.com",
        ["SubscriptionKey"] = "subscription-secret",
        ["ApiUser"] = Guid.NewGuid().ToString(),
        ["ApiKey"] = "api-secret",
        ["TargetEnvironment"] = "sandbox"
    };

    private sealed class NullExchangeWriter : IPaymentApiExchangeWriter
    {
        public Task WriteAsync(PaymentApiExchangeWrite exchange, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage source)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri);
        foreach (var header in source.Headers) clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (source.Content is not null)
        {
            clone.Content = new ByteArrayContent(await source.Content.ReadAsByteArrayAsync());
            foreach (var header in source.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }

    private sealed class StaticHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => handler(request);
    }

    private static PaymentProviderConfiguration ProviderConfiguration(string key, string name, bool enabled) => new()
    {
        Id = Guid.NewGuid(),
        ProviderKey = key,
        DisplayName = name,
        ProviderKind = key == "DEVELOPMENT_SANDBOX" ? PaymentProviderKinds.Sandbox : PaymentProviderKinds.Direct,
        Environment = "SANDBOX",
        IsEnabled = enabled,
        ProtectedConfiguration = "protected"
    };

    private static PaymentProviderRoute Route(Guid methodId, Guid providerId, int priority, string providerMethod) => new()
    {
        Id = Guid.NewGuid(),
        PaymentMethodDefinitionId = methodId,
        PaymentProviderConfigurationId = providerId,
        ProviderPaymentMethodCode = providerMethod,
        Priority = priority,
        IsEnabled = true
    };

    private sealed class PaymentFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private PaymentFixture(SqliteConnection connection, AppDbContext db)
        {
            _connection = connection;
            Db = db;
        }

        public AppDbContext Db { get; }

        public static async Task<PaymentFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .UseSnakeCaseNamingConvention()
                .Options;
            var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();
            return new PaymentFixture(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
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
