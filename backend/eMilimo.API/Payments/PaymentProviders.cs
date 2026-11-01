using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using eMilimo.Core.Payments;

namespace eMilimo.API.Payments;

public sealed class PaymentProviderRegistry : IPaymentProviderRegistry
{
    private readonly IReadOnlyDictionary<string, IPaymentProvider> _providers;

    public PaymentProviderRegistry(IEnumerable<IPaymentProvider> providers)
    {
        var materialized = providers.ToList();
        var duplicate = materialized
            .GroupBy(x => x.Descriptor.ProviderKey, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Payment provider key '{duplicate.Key}' is registered more than once.");

        _providers = materialized.ToDictionary(
            x => x.Descriptor.ProviderKey,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<PaymentProviderDescriptor> DescribeAll() =>
        _providers.Values.Select(x => x.Descriptor).OrderBy(x => x.DisplayName).ToArray();

    public IPaymentProvider GetRequired(string providerKey) =>
        _providers.TryGetValue(providerKey, out var provider)
            ? provider
            : throw new KeyNotFoundException($"Payment provider '{providerKey}' is not registered.");
}

public sealed class AirtelMoneyPaymentProvider : UnavailablePaymentProvider
{
    public override PaymentProviderDescriptor Descriptor { get; } = new(
        "AIRTEL_MONEY_DIRECT",
        "Airtel Money (Direct)",
        PaymentProviderKinds.Direct,
        IsImplemented: false,
        IsProductionReady: false,
        ["AIRTEL_MONEY"],
        "Awaiting approved Airtel Money API documentation, credentials and webhook-signature specification.");
}

public sealed class MtnMomoPaymentProvider : IPaymentProvider, IPaymentProviderDiagnostics
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MtnMomoPaymentProvider> _logger;
    private readonly PaymentProviderEndpointPolicy _endpointPolicy;
    private readonly IPaymentApiExchangeWriter _diagnostics;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ConcurrentDictionary<string, CachedToken> _tokens = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tokenLocks = new(StringComparer.Ordinal);

    public MtnMomoPaymentProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<MtnMomoPaymentProvider> logger,
        PaymentProviderEndpointPolicy endpointPolicy,
        IPaymentApiExchangeWriter diagnostics,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _endpointPolicy = endpointPolicy;
        _diagnostics = diagnostics;
        _httpContextAccessor = httpContextAccessor;
    }

    public PaymentProviderDescriptor Descriptor { get; } = new(
        "MTN_MOMO_DIRECT",
        "MTN MoMo (Direct)",
        PaymentProviderKinds.Direct,
        IsImplemented: true,
        IsProductionReady: false,
        ["MTN_MOMO"],
        "Sandbox Collections is implemented. Production activation remains blocked until MTN Zambia onboarding and production credentials are approved.");

    public async Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = Settings.Required(request.ProviderSettings, _endpointPolicy);
        if (settings.IsSandbox && !string.Equals(request.CurrencyCode, "EUR", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("MTN MoMo sandbox accepts EUR only. Use an isolated EUR test configuration; do not treat sandbox payments as ZMW receipts.");
        if (!settings.SupportedCurrencies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(request.CurrencyCode, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Currency {request.CurrencyCode} is not enabled for this MTN profile.");

        if (!string.IsNullOrWhiteSpace(settings.CallbackHost))
            _endpointPolicy.ValidateCallback(request.CallbackUrl, settings.CallbackHost, settings.Environment);
        var referenceId = request.PaymentTransactionId.ToString("D");
        var payerMessage = LimitMessage(SettingOrDefault(request.ProviderSettings, "PayerMessage", "eMilimo Digital Giving"), "Payer message");
        var payeeNote = LimitMessage(SettingOrDefault(request.ProviderSettings, "PayeeNote", request.PaymentReference), "Payee note");
        var payload = JsonSerializer.Serialize(new
        {
            amount = request.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            currency = request.CurrencyCode.ToUpperInvariant(),
            externalId = referenceId,
            payer = new { partyIdType = "MSISDN", partyId = NormalizeMsisdn(request.PayerAddress, settings) },
            payerMessage,
            payeeNote
        });
        var token = await GetAccessTokenAsync(settings, cancellationToken);
        using var message = new HttpRequestMessage(HttpMethod.Post, BuildUri(settings.ApiBaseUrl, settings.RequestToPayEndpointPath));
        ApplyCollectionHeaders(message, settings, token);
        message.Headers.Add("X-Reference-Id", referenceId);
        if (!string.IsNullOrWhiteSpace(settings.CallbackHost))
            message.Headers.Add("X-Callback-Url", request.CallbackUrl.ToString());
        message.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await SendAsync(message, settings, "REQUEST_TO_PAY", request.PaymentTransactionId, referenceId, payload, isInitiation: true, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var existing = await QueryDetailedAsync(referenceId, settings, cancellationToken);
            return new PaymentInitiationResult(existing.Status, referenceId,
                "MTN MoMo had already accepted this payment reference; its current status was recovered safely.", null);
        }
        if (response.StatusCode != HttpStatusCode.Accepted)
            throw ProviderException("MTN MoMo rejected the request-to-pay operation", response);

        return new PaymentInitiationResult(
            PaymentTransactionStatuses.Pending,
            referenceId,
            "Payment request sent. Approve the MTN MoMo prompt on the payer's phone.",
            DateTime.UtcNow.AddMinutes(15));
    }

    public async Task<PaymentStatusResult> QueryStatusAsync(
        string providerTransactionId,
        IReadOnlyDictionary<string, string> providerSettings,
        CancellationToken cancellationToken = default)
    {
        var detail = await QueryDetailedAsync(providerTransactionId, Settings.Required(providerSettings, _endpointPolicy), cancellationToken);
        return new PaymentStatusResult(
            detail.Status,
            providerTransactionId,
            null,
            detail.Status == PaymentTransactionStatuses.Succeeded ? detail.Amount : null,
            detail.Status == PaymentTransactionStatuses.Succeeded ? DateTime.UtcNow : null,
            detail.Message,
            detail.Amount,
            detail.Currency);
    }

    public async Task<PaymentWebhookResult> VerifyAndParseWebhookAsync(
        PaymentWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var callback = JsonDocument.Parse(request.RawBody);
            var root = callback.RootElement;
            var referenceId = StringValue(root, "externalId");
            if (string.IsNullOrWhiteSpace(referenceId) || !Guid.TryParse(referenceId, out _))
                return InvalidWebhook(request.RawBody, "MTN MoMo callback does not contain a valid external transaction reference.");

            // MTN callbacks do not carry a partner-verifiable signature. Treat the
            // callback only as a signal and authenticate its result by querying the
            // Collections status endpoint with our encrypted credentials.
            var verified = await QueryDetailedAsync(referenceId, Settings.Required(request.ProviderSettings, _endpointPolicy), cancellationToken);
            var callbackStatus = MapStatus(StringValue(root, "status"));
            var callbackAmount = DecimalValue(root, "amount");
            var callbackCurrency = StringValue(root, "currency");
            var matches = callbackStatus == verified.Status &&
                          (!callbackAmount.HasValue || callbackAmount == verified.Amount) &&
                          (string.IsNullOrWhiteSpace(callbackCurrency) || string.Equals(callbackCurrency, verified.Currency, StringComparison.OrdinalIgnoreCase));
            if (!matches)
                return InvalidWebhook(request.RawBody, "MTN MoMo callback did not match the provider status query.", referenceId);

            return new PaymentWebhookResult(
                true,
                $"{referenceId}:{verified.Status}:{verified.FinancialTransactionId ?? "none"}",
                referenceId,
                verified.Status,
                verified.Amount,
                verified.Currency,
                null,
                verified.Status == PaymentTransactionStatuses.Succeeded ? verified.Amount : null,
                verified.Status == PaymentTransactionStatuses.Succeeded ? DateTime.UtcNow : null,
                null);
        }
        catch (JsonException)
        {
            return InvalidWebhook(request.RawBody, "MTN MoMo callback payload is invalid JSON.");
        }
    }

    public async Task<PaymentProviderConnectionTestResult> TestConnectionAsync(
        IReadOnlyDictionary<string, string> providerSettings,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var settings = Settings.Required(providerSettings, _endpointPolicy);
            InvalidateToken(settings);
            await GetAccessTokenAsync(settings, cancellationToken);
            return new PaymentProviderConnectionTestResult(true, "MTN OAuth authentication succeeded.", 200, null, stopwatch.ElapsedMilliseconds);
        }
        catch (PaymentProviderException ex)
        {
            return new PaymentProviderConnectionTestResult(false, ex.Message, ex.HttpStatusCode, ex.ErrorCategory, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MTN connection test failed before a provider response was received.");
            return new PaymentProviderConnectionTestResult(false, "MTN connection test failed.", null, "CONFIGURATION", stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<MtnStatus> QueryDetailedAsync(
        string referenceId,
        Settings settings,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(settings, cancellationToken);
        var statusPath = settings.StatusEndpointTemplate.Replace("{referenceId}", Uri.EscapeDataString(referenceId), StringComparison.Ordinal);
        using var message = new HttpRequestMessage(HttpMethod.Get, BuildUri(settings.ApiBaseUrl, statusPath));
        ApplyCollectionHeaders(message, settings, token);
        var transactionId = Guid.TryParse(referenceId, out var parsedId) ? parsedId : (Guid?)null;
        var response = await SendAsync(message, settings, "PAYMENT_STATUS", transactionId, referenceId, null, false, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw ProviderException("MTN MoMo could not return the payment status", response);
        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        var providerStatus = StringValue(root, "status");
        return new MtnStatus(
            MapStatus(providerStatus),
            DecimalValue(root, "amount"),
            StringValue(root, "currency"),
            StringValue(root, "financialTransactionId"),
            Reason(root) ?? providerStatus);
    }

    private async Task<string> GetAccessTokenAsync(Settings settings, CancellationToken cancellationToken)
    {
        var cacheKey = settings.CacheKey;
        if (_tokens.TryGetValue(cacheKey, out var cached) && cached.ExpiresAtUtc > DateTime.UtcNow.AddSeconds(60))
            return cached.AccessToken;
        var gate = _tokenLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_tokens.TryGetValue(cacheKey, out cached) && cached.ExpiresAtUtc > DateTime.UtcNow.AddSeconds(60))
                return cached.AccessToken;
            using var message = new HttpRequestMessage(HttpMethod.Post, BuildUri(settings.ApiBaseUrl, settings.TokenEndpointPath));
            message.Headers.Add("Ocp-Apim-Subscription-Key", settings.SubscriptionKey);
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.ApiUser}:{settings.ApiKey}")));
            // MTN's token endpoint does not accept a request body. Do not assign HttpContent here.
            var response = await SendAsync(message, settings, "OAUTH_TOKEN", null, null, null, false, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw ProviderException("MTN MoMo access-token request failed", response);
            using var document = JsonDocument.Parse(response.Body);
            var token = StringValue(document.RootElement, "access_token");
            if (string.IsNullOrWhiteSpace(token))
                throw new PaymentProviderException("MTN MoMo returned an access-token response without an access token.", "INVALID_RESPONSE");
            var expiresIn = document.RootElement.TryGetProperty("expires_in", out var expiry) && expiry.TryGetInt32(out var seconds)
                ? Math.Clamp(seconds, 60, 86400)
                : 3600;
            _tokens[cacheKey] = new CachedToken(token, DateTime.UtcNow.AddSeconds(expiresIn));
            return token;
        }
        finally { gate.Release(); }
    }

    private void ApplyCollectionHeaders(HttpRequestMessage message, Settings settings, string token)
    {
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.Add("Ocp-Apim-Subscription-Key", settings.SubscriptionKey);
        message.Headers.Add("X-Target-Environment", settings.TargetEnvironment);
    }

    private HttpClient Client() => _httpClientFactory.CreateClient("mtn-momo");

    private static Uri BuildUri(string baseUrl, string path) => new(new Uri(baseUrl.TrimEnd('/') + "/"), path.TrimStart('/'));

    private static string NormalizeMsisdn(string value, Settings settings)
    {
        var normalized = new string(value.Where(char.IsDigit).ToArray());
        if (settings.IsSandbox)
        {
            if (normalized.Length is < 8 or > 15)
                throw new InvalidOperationException("MTN sandbox MSISDN must contain 8 to 15 digits.");
            return normalized;
        }
        if (normalized.StartsWith('0') && settings.NationalLength > 0 && normalized.Length == settings.NationalLength + 1)
            normalized = settings.CountryCode + normalized[1..];
        else if (settings.NationalLength > 0 && normalized.Length == settings.NationalLength)
            normalized = settings.CountryCode + normalized;
        var expectedLength = settings.CountryCode.Length + settings.NationalLength;
        if (string.IsNullOrWhiteSpace(settings.CountryCode) || expectedLength <= settings.CountryCode.Length ||
            normalized.Length != expectedLength || !normalized.StartsWith(settings.CountryCode, StringComparison.Ordinal))
            throw new InvalidOperationException($"MTN Zambia MSISDN must be in international format {settings.CountryCode} followed by {settings.NationalLength} national digits.");
        var national = normalized[settings.CountryCode.Length..];
        if (settings.AllowedPrefixes.Length > 0 && !settings.AllowedPrefixes.Any(national.StartsWith))
            throw new InvalidOperationException("The payer number is not within an approved MTN Zambia mobile prefix.");
        return normalized;
    }

    private static string LimitMessage(string value, string label)
    {
        if (value.Length > 160) throw new InvalidOperationException($"{label} cannot exceed 160 characters.");
        if (value.Any(char.IsControl)) throw new InvalidOperationException($"{label} contains unsupported control characters.");
        return value;
    }

    private static string SettingOrDefault(IReadOnlyDictionary<string, string> settings, string key, string fallback) =>
        settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;

    private static string MapStatus(string? status) => status?.Trim().ToUpperInvariant() switch
    {
        "SUCCESSFUL" => PaymentTransactionStatuses.Succeeded,
        "FAILED" or "REJECTED" => PaymentTransactionStatuses.Failed,
        "PENDING" or "ONGOING" => PaymentTransactionStatuses.Pending,
        "EXPIRED" => PaymentTransactionStatuses.Expired,
        _ => PaymentTransactionStatuses.Unknown
    };

    private static string? StringValue(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static decimal? DecimalValue(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
        return value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out number)
            ? number
            : null;
    }

    private static string? Reason(JsonElement root)
    {
        if (!root.TryGetProperty("reason", out var reason)) return null;
        return reason.ValueKind == JsonValueKind.String
            ? reason.GetString()
            : reason.ValueKind == JsonValueKind.Object ? StringValue(reason, "message") ?? StringValue(reason, "code") : null;
    }

    private static PaymentWebhookResult InvalidWebhook(string rawBody, string reason, string? referenceId = null) => new(
        false,
        $"INVALID-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant()}",
        referenceId,
        PaymentTransactionStatuses.Unknown,
        null,
        null,
        null,
        null,
        null,
        reason);

    private PaymentProviderException ProviderException(string operation, ProviderHttpResponse response)
    {
        var providerCode = "";
        try
        {
            using var document = JsonDocument.Parse(response.Body);
            providerCode = StringValue(document.RootElement, "code") ?? StringValue(document.RootElement, "message") ?? "";
        }
        catch (JsonException) { }
        var category = Classify(response.StatusCode);
        _logger.LogWarning("{Operation}. MTN returned HTTP {StatusCode} ({ProviderCode}, {Category}).", operation, (int)response.StatusCode, providerCode, category);
        var suffix = string.IsNullOrWhiteSpace(providerCode) ? "" : $" ({providerCode})";
        return new PaymentProviderException($"{operation}: HTTP {(int)response.StatusCode}{suffix}.", category,
            isAmbiguous: response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout,
            (int)response.StatusCode, providerCode);
    }

    private async Task<ProviderHttpResponse> SendAsync(
        HttpRequestMessage message,
        Settings settings,
        string operation,
        Guid? transactionId,
        string? transactionReference,
        string? requestPayload,
        bool isInitiation,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        try
        {
            using var response = await Client().SendAsync(message, HttpCompletionOption.ResponseContentRead, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            var result = new ProviderHttpResponse(response.StatusCode, body,
                response.Headers.Concat(response.Content.Headers).ToDictionary(x => x.Key, x => string.Join(',', x.Value), StringComparer.OrdinalIgnoreCase));
            await RecordAsync(message, settings, operation, transactionId, transactionReference, requestPayload, result, started.ElapsedMilliseconds, null, cancellationToken);
            return result;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            await RecordAsync(message, settings, operation, transactionId, transactionReference, requestPayload, null, started.ElapsedMilliseconds, "TIMEOUT", CancellationToken.None);
            throw new PaymentProviderException("The MTN request timed out. Its final provider state is not yet known.", "TIMEOUT", isInitiation, innerException: ex);
        }
        catch (HttpRequestException ex)
        {
            await RecordAsync(message, settings, operation, transactionId, transactionReference, requestPayload, null, started.ElapsedMilliseconds, "NETWORK", CancellationToken.None);
            throw new PaymentProviderException("The MTN endpoint could not be reached. Its final provider state is not yet known.", "NETWORK", isInitiation, innerException: ex);
        }
    }

    private Task RecordAsync(HttpRequestMessage message, Settings settings, string operation, Guid? transactionId,
        string? reference, string? requestPayload, ProviderHttpResponse? response, long duration, string? error, CancellationToken cancellationToken)
    {
        var requestHeaders = message.Headers.Concat(message.Content is null
                ? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()
                : message.Content.Headers)
            .ToDictionary(x => x.Key, x => string.Join(',', x.Value), StringComparer.OrdinalIgnoreCase);
        return _diagnostics.WriteAsync(new PaymentApiExchangeWrite(
            settings.ConfigurationId, settings.ConfigurationVersion, settings.Environment, operation,
            message.Method.Method, message.RequestUri!, transactionId, reference, requestHeaders, requestPayload,
            response is null ? null : (int)response.StatusCode, response?.Headers, response?.Body,
            duration, 1, error ?? (response is null || response.IsSuccessStatusCode ? null : Classify(response.StatusCode)),
            _httpContextAccessor.HttpContext?.TraceIdentifier), cancellationToken);
    }

    private static string Classify(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => "VALIDATION",
        HttpStatusCode.Unauthorized => "AUTHENTICATION",
        HttpStatusCode.Forbidden => "AUTHORIZATION",
        HttpStatusCode.NotFound => "NOT_FOUND",
        HttpStatusCode.Conflict => "DUPLICATE_REFERENCE",
        HttpStatusCode.TooManyRequests => "RATE_LIMIT",
        HttpStatusCode.RequestTimeout or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout => "TEMPORARY_PROVIDER_FAILURE",
        >= HttpStatusCode.InternalServerError => "PROVIDER_FAILURE",
        _ => "HTTP_ERROR"
    };

    private void InvalidateToken(Settings settings) => _tokens.TryRemove(settings.CacheKey, out _);

    private sealed record MtnStatus(
        string Status,
        decimal? Amount,
        string? Currency,
        string? FinancialTransactionId,
        string? Message);

    private sealed record CachedToken(string AccessToken, DateTime ExpiresAtUtc);
    private sealed record ProviderHttpResponse(HttpStatusCode StatusCode, string Body, IReadOnlyDictionary<string, string> Headers)
    {
        public bool IsSuccessStatusCode => (int)StatusCode is >= 200 and <= 299;
    }

    private sealed record Settings(
        string ApiBaseUrl,
        string SubscriptionKey,
        string ApiUser,
        string ApiKey,
        string TargetEnvironment,
        string Environment,
        string TokenEndpointPath,
        string RequestToPayEndpointPath,
        string StatusEndpointTemplate,
        string SupportedCurrencies,
        string CountryCode,
        int NationalLength,
        string[] AllowedPrefixes,
        int TimeoutSeconds,
        string? CallbackHost,
        Guid ConfigurationId,
        int ConfigurationVersion)
    {
        public bool IsSandbox => string.Equals(TargetEnvironment, "sandbox", StringComparison.OrdinalIgnoreCase);

        public string CacheKey => ConfigurationId != Guid.Empty
            ? $"{ConfigurationId:N}:{ConfigurationVersion}"
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{ApiBaseUrl}|{ApiUser}|{TargetEnvironment}")));

        public static Settings Required(IReadOnlyDictionary<string, string> values, PaymentProviderEndpointPolicy endpointPolicy)
        {
            string Get(string key) => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : throw new InvalidOperationException($"MTN MoMo setting '{key}' is required.");
            var environment = values.TryGetValue("_Environment", out var configuredEnvironment) ? configuredEnvironment :
                string.Equals(Get("TargetEnvironment"), "sandbox", StringComparison.OrdinalIgnoreCase) ? "SANDBOX" : "PRODUCTION";
            var withDefaults = MtnMomoProfile.ApplyDefaults(values, environment);
            string Value(string key) => withDefaults.TryGetValue(key, out var value) ? value.Trim() : string.Empty;
            var baseUrl = Get("ApiBaseUrl");
            endpointPolicy.ValidateBaseUri(MtnMomoProfile.ProviderKey, environment, baseUrl);
            var currencies = Value("SupportedCurrencies").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var result = new Settings(
                baseUrl, Get("SubscriptionKey"), Get("ApiUser"), Get("ApiKey"), Get("TargetEnvironment"), environment,
                endpointPolicy.ValidateRelativePath(Value("TokenEndpointPath"), "Token endpoint"),
                endpointPolicy.ValidateRelativePath(Value("RequestToPayEndpointPath"), "Request to Pay endpoint"),
                endpointPolicy.ValidateRelativePath(Value("StatusEndpointTemplate"), "Status endpoint", true),
                string.Join(',', currencies), Value("MsisdnCountryCode"), ParseInt(Value("MsisdnNationalLength"), 0, 15, "MSISDN national length"),
                Value("MsisdnAllowedPrefixes").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                ParseInt(Value("TimeoutSeconds"), 5, 120, "Timeout"),
                Value("CallbackHost") is { Length: > 0 } callbackHost ? callbackHost : null,
                Guid.TryParse(Value("_ConfigurationId"), out var configurationId) ? configurationId : Guid.Empty,
                int.TryParse(Value("_ConfigurationVersion"), out var configurationVersion) ? configurationVersion : 1);
            if (!Guid.TryParse(result.ApiUser, out _)) throw new InvalidOperationException("MTN MoMo API User must be a valid UUID.");
            if (!currencies.Contains(result.IsSandbox ? "EUR" : "ZMW", StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"MTN {environment} profile does not include its required currency.");
            return result;
        }

        private static int ParseInt(string value, int minimum, int maximum, string label) =>
            int.TryParse(value, out var parsed) && parsed >= minimum && parsed <= maximum
                ? parsed
                : throw new InvalidOperationException($"{label} must be between {minimum} and {maximum}.");
    }
}

public sealed class ZamtelMoneyPaymentProvider : UnavailablePaymentProvider
{
    public override PaymentProviderDescriptor Descriptor { get; } = new(
        "ZAMTEL_MONEY_DIRECT",
        "Zamtel Money (Direct)",
        PaymentProviderKinds.Direct,
        IsImplemented: false,
        IsProductionReady: false,
        ["ZAMTEL_MONEY"],
        "Awaiting approved Zamtel Money API documentation, credentials and webhook-signature specification.");
}

public abstract class UnavailablePaymentProvider : IPaymentProvider
{
    public abstract PaymentProviderDescriptor Descriptor { get; }

    public Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromException<PaymentInitiationResult>(Unavailable());

    public Task<PaymentStatusResult> QueryStatusAsync(
        string providerTransactionId,
        IReadOnlyDictionary<string, string> providerSettings,
        CancellationToken cancellationToken = default) =>
        Task.FromException<PaymentStatusResult>(Unavailable());

    public Task<PaymentWebhookResult> VerifyAndParseWebhookAsync(
        PaymentWebhookRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromException<PaymentWebhookResult>(Unavailable());

    private NotSupportedException Unavailable() => new(
        Descriptor.UnavailableReason ?? $"{Descriptor.DisplayName} is not available in this build.");
}

/// <summary>
/// Development-only provider. It never reports a payment as successful by
/// itself; a signed sandbox webhook is required, preventing mock transactions
/// from being presented as genuine provider receipts.
/// </summary>
public sealed class DevelopmentSandboxPaymentProvider : IPaymentProvider
{
    public PaymentProviderDescriptor Descriptor { get; } = new(
        "DEVELOPMENT_SANDBOX",
        "Development Payment Sandbox",
        PaymentProviderKinds.Sandbox,
        IsImplemented: true,
        IsProductionReady: false,
        ["MOBILE_MONEY", "AIRTEL_MONEY", "MTN_MOMO", "ZAMTEL_MONEY", "POS_CARD"],
        "Available only in the Development environment; never use for genuine payments.");

    public Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        var providerReference = $"SANDBOX-{Guid.NewGuid():N}";
        return Task.FromResult(new PaymentInitiationResult(
            PaymentTransactionStatuses.Pending,
            providerReference,
            "Development sandbox request created. No real funds were collected.",
            DateTime.UtcNow.AddMinutes(15)));
    }

    public Task<PaymentStatusResult> QueryStatusAsync(
        string providerTransactionId,
        IReadOnlyDictionary<string, string> providerSettings,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentStatusResult(
            PaymentTransactionStatuses.Pending,
            providerTransactionId,
            null,
            null,
            null,
            "Sandbox transactions remain pending until a signed development webhook is received."));

    public Task<PaymentWebhookResult> VerifyAndParseWebhookAsync(
        PaymentWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        request.ProviderSettings.TryGetValue("WebhookSecret", out var expectedSecret);
        request.Headers.TryGetValue("X-eMilimo-Sandbox-Signature", out var suppliedSignature);
        var expectedSignature = string.IsNullOrWhiteSpace(expectedSecret)
            ? string.Empty
            : Convert.ToHexString(HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(expectedSecret),
                Encoding.UTF8.GetBytes(request.RawBody))).ToLowerInvariant();
        var authentic = !string.IsNullOrWhiteSpace(expectedSignature) &&
                        FixedTimeEquals(expectedSignature, suppliedSignature ?? string.Empty);

        try
        {
            using var document = JsonDocument.Parse(request.RawBody);
            var root = document.RootElement;
            return Task.FromResult(new PaymentWebhookResult(
                authentic,
                RequiredString(root, "eventKey"),
                RequiredString(root, "providerTransactionId"),
                RequiredString(root, "status").ToUpperInvariant(),
                root.TryGetProperty("amount", out var amount) ? amount.GetDecimal() : null,
                root.TryGetProperty("currencyCode", out var currency) ? currency.GetString() : null,
                root.TryGetProperty("providerFee", out var fee) ? fee.GetDecimal() : null,
                root.TryGetProperty("netSettlementAmount", out var net) ? net.GetDecimal() : null,
                root.TryGetProperty("completedAtUtc", out var completed) && completed.TryGetDateTime(out var completedAt)
                    ? DateTime.SpecifyKind(completedAt, DateTimeKind.Utc)
                    : null,
                authentic ? null : "Sandbox webhook signature validation failed."));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return Task.FromResult(new PaymentWebhookResult(
                false,
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.RawBody))).ToLowerInvariant(),
                null,
                PaymentTransactionStatuses.Unknown,
                null,
                null,
                null,
                null,
                null,
                "Sandbox webhook payload is invalid."));
        }
    }

    private static string RequiredString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!
            : throw new InvalidOperationException($"Sandbox webhook property '{propertyName}' is required.");

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
