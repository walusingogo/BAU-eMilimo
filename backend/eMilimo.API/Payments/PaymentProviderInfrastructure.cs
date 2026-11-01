using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed class PaymentProviderException : InvalidOperationException
{
    public PaymentProviderException(
        string message,
        string errorCategory,
        bool isAmbiguous = false,
        int? httpStatusCode = null,
        string? providerCode = null,
        Exception? innerException = null) : base(message, innerException)
    {
        ErrorCategory = errorCategory;
        IsAmbiguous = isAmbiguous;
        HttpStatusCode = httpStatusCode;
        ProviderCode = providerCode;
    }

    public string ErrorCategory { get; }
    public bool IsAmbiguous { get; }
    public int? HttpStatusCode { get; }
    public string? ProviderCode { get; }
}

public sealed record PaymentApiExchangeWrite(
    Guid ProviderConfigurationId,
    int ConfigurationVersion,
    string Environment,
    string Operation,
    string HttpMethod,
    Uri Endpoint,
    Guid? PaymentTransactionId,
    string? TransactionReference,
    IReadOnlyDictionary<string, string>? RequestHeaders,
    string? RequestPayload,
    int? HttpStatusCode,
    IReadOnlyDictionary<string, string>? ResponseHeaders,
    string? ResponsePayload,
    long DurationMilliseconds,
    int AttemptNumber,
    string? ErrorCategory,
    string? CorrelationId);

public interface IPaymentApiExchangeWriter
{
    Task WriteAsync(PaymentApiExchangeWrite exchange, CancellationToken cancellationToken = default);
}

public sealed class PaymentApiExchangeWriter(
    IDbContextFactory<AppDbContext> dbFactory,
    ILogger<PaymentApiExchangeWriter> logger) : IPaymentApiExchangeWriter
{
    public async Task WriteAsync(PaymentApiExchangeWrite exchange, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var endpoint = exchange.Endpoint.GetLeftPart(UriPartial.Path);
            var transactionId = exchange.PaymentTransactionId.HasValue &&
                                await db.DigitalCollections.AnyAsync(x => x.Id == exchange.PaymentTransactionId.Value, cancellationToken)
                ? exchange.PaymentTransactionId
                : null;
            db.PaymentApiExchanges.Add(new PaymentApiExchange
            {
                Id = Guid.NewGuid(),
                PaymentProviderConfigurationId = exchange.ProviderConfigurationId,
                DigitalCollectionId = transactionId,
                ConfigurationVersion = exchange.ConfigurationVersion,
                Environment = Truncate(exchange.Environment.ToUpperInvariant(), 30),
                Operation = Truncate(exchange.Operation.ToUpperInvariant(), 50),
                HttpMethod = Truncate(exchange.HttpMethod.ToUpperInvariant(), 10),
                Endpoint = Truncate(endpoint, 500),
                TransactionReference = TruncateNullable(exchange.TransactionReference, 80),
                SanitizedRequestHeaders = PaymentDiagnosticSanitizer.Headers(exchange.RequestHeaders),
                SanitizedRequestPayload = PaymentDiagnosticSanitizer.Payload(exchange.RequestPayload),
                HttpStatusCode = exchange.HttpStatusCode,
                SanitizedResponseHeaders = PaymentDiagnosticSanitizer.Headers(exchange.ResponseHeaders),
                SanitizedResponsePayload = PaymentDiagnosticSanitizer.Payload(exchange.ResponsePayload),
                DurationMilliseconds = Math.Max(0, exchange.DurationMilliseconds),
                AttemptNumber = Math.Max(1, exchange.AttemptNumber),
                ErrorCategory = TruncateNullable(exchange.ErrorCategory, 50),
                CorrelationId = TruncateNullable(exchange.CorrelationId, 100),
                RequestedAtUtc = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Version = 1
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Diagnostics must never change the financial outcome.
            logger.LogError(ex, "Payment API exchange diagnostics could not be persisted for {Operation}.", exchange.Operation);
        }
    }

    private static string Truncate(string value, int maximum) => value.Length <= maximum ? value : value[..maximum];
    private static string? TruncateNullable(string? value, int maximum) =>
        string.IsNullOrWhiteSpace(value) ? null : Truncate(value.Trim(), maximum);
}

public static class PaymentDiagnosticSanitizer
{
    private static readonly HashSet<string> SecretNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization", "ocp-apim-subscription-key", "subscriptionkey", "apikey", "api-key",
        "clientsecret", "password", "access_token", "refresh_token", "token", "secret"
    };

    public static string Headers(IReadOnlyDictionary<string, string>? headers)
    {
        if (headers is null || headers.Count == 0) return "{}";
        return JsonSerializer.Serialize(headers.ToDictionary(
            x => x.Key,
            x => IsSecret(x.Key) ? "[REDACTED]" : Truncate(x.Value, 500),
            StringComparer.OrdinalIgnoreCase));
    }

    public static string? Payload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;
        try
        {
            var node = JsonNode.Parse(payload);
            Redact(node, null);
            return Truncate(node?.ToJsonString() ?? "{}", 16000);
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { text = Truncate(payload, 2000) });
        }
    }

    private static void Redact(JsonNode? node, string? propertyName)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(x => x.Key).ToArray())
            {
                if (IsSecret(key)) obj[key] = "[REDACTED]";
                else if (key.Equals("partyId", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("phone", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("msisdn", StringComparison.OrdinalIgnoreCase))
                    obj[key] = MaskPhone(obj[key]?.ToString());
                else Redact(obj[key], key);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array) Redact(item, propertyName);
        }
    }

    private static bool IsSecret(string key) => SecretNames.Contains(key) || SecretNames.Any(x => key.Contains(x, StringComparison.OrdinalIgnoreCase));

    private static string MaskPhone(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length <= 4 ? "****" : $"***{digits[^4..]}";
    }

    private static string Truncate(string value, int maximum) => value.Length <= maximum ? value : value[..maximum];
}

public sealed class PaymentProviderEndpointPolicy(IConfiguration configuration)
{
    private static readonly IReadOnlyDictionary<string, string[]> BuiltInHosts =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["MTN_MOMO_DIRECT"] = ["sandbox.momodeveloper.mtn.com", "proxy.momoapi.mtn.com"]
        };

    public Uri ValidateBaseUri(string providerKey, string environment, string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("Provider Base URL must be a clean absolute HTTPS URL without credentials, query strings or fragments.");
        if (IPAddress.TryParse(uri.Host, out var literal) && IsNonPublic(literal))
            throw new InvalidOperationException("Private, loopback and link-local provider endpoints are not permitted.");
        if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Local provider endpoints are not permitted.");

        var approved = new HashSet<string>(BuiltInHosts.GetValueOrDefault(providerKey) ?? [], StringComparer.OrdinalIgnoreCase);
        foreach (var host in configuration.GetSection($"Payments:ApprovedApiHosts:{providerKey}").Get<string[]>() ?? [])
            approved.Add(host.Trim());
        if (!approved.Contains(uri.IdnHost))
            throw new InvalidOperationException($"The host '{uri.IdnHost}' is not approved for {providerKey}.");

        var normalizedEnvironment = environment.Trim().ToUpperInvariant();
        if (providerKey.Equals("MTN_MOMO_DIRECT", StringComparison.OrdinalIgnoreCase))
        {
            if (normalizedEnvironment == "SANDBOX" && !uri.IdnHost.Equals("sandbox.momodeveloper.mtn.com", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("MTN sandbox must use sandbox.momodeveloper.mtn.com.");
            if (normalizedEnvironment == "PRODUCTION" && uri.IdnHost.Equals("sandbox.momodeveloper.mtn.com", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A production profile cannot use the MTN sandbox host.");
        }
        return uri;
    }

    public string ValidateRelativePath(string value, string label, bool isTemplate = false)
    {
        var path = value?.Trim() ?? string.Empty;
        if (!path.StartsWith('/') || path.StartsWith("//") || path.Contains("..", StringComparison.Ordinal) ||
            path.Contains('\\') || Uri.TryCreate(path, UriKind.Absolute, out _))
            throw new InvalidOperationException($"{label} must be a safe relative API path.");
        if (isTemplate && !path.Contains("{referenceId}", StringComparison.Ordinal))
            throw new InvalidOperationException($"{label} must contain the {{referenceId}} placeholder.");
        return path;
    }

    public void ValidateCallback(Uri callback, string? configuredHost, string environment)
    {
        if (!callback.IsAbsoluteUri || callback.Scheme is not ("http" or "https") || callback.IsLoopback)
            throw new InvalidOperationException("The payment callback URL must be an externally reachable absolute URL.");
        if (!string.IsNullOrWhiteSpace(configuredHost) && !callback.IdnHost.Equals(configuredHost.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The callback URL host must exactly match the provider callback host; subdomains are not accepted by MTN.");
        if (callback.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("MTN callbacks must use HTTPS in sandbox and production.");
    }

    private static bool IsNonPublic(IPAddress address) =>
        IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
        address.IsIPv6LinkLocal || address.IsIPv6SiteLocal ||
        address.AddressFamily == AddressFamily.InterNetwork && IsPrivateV4(address.GetAddressBytes());

    private static bool IsPrivateV4(byte[] bytes) => bytes[0] == 10 || bytes[0] == 127 ||
        bytes[0] == 169 && bytes[1] == 254 || bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
        bytes[0] == 192 && bytes[1] == 168 || bytes[0] == 0;
}

public static class MtnMomoProfile
{
    public const string ProviderKey = "MTN_MOMO_DIRECT";

    public static Dictionary<string, string> ApplyDefaults(IReadOnlyDictionary<string, string> source, string environment)
    {
        var values = new Dictionary<string, string>(source, StringComparer.OrdinalIgnoreCase);
        var sandbox = environment.Equals("SANDBOX", StringComparison.OrdinalIgnoreCase);
        Default("ApiBaseUrl", sandbox ? "https://sandbox.momodeveloper.mtn.com" : "https://proxy.momoapi.mtn.com");
        Default("TokenEndpointPath", "/collection/token/");
        Default("RequestToPayEndpointPath", "/collection/v1_0/requesttopay");
        Default("StatusEndpointTemplate", "/collection/v1_0/requesttopay/{referenceId}");
        Default("BalanceEndpointPath", "/collection/v1_0/account/balance");
        Default("AccountHolderEndpointTemplate", "/collection/v1_0/accountholder/msisdn/{referenceId}/active");
        Default("ApiVersion", "v1_0");
        Default("TargetEnvironment", sandbox ? "sandbox" : "mtnzambia");
        Default("SupportedCurrencies", sandbox ? "EUR" : "ZMW");
        Default("ApprovedApiHost", sandbox ? "sandbox.momodeveloper.mtn.com" : "proxy.momoapi.mtn.com");
        Default("MsisdnCountryCode", sandbox ? "" : "260");
        Default("MsisdnNationalLength", sandbox ? "0" : "9");
        Default("TimeoutSeconds", "30");
        Default("PollingInitialSeconds", "20");
        Default("PollingMaximumSeconds", "300");
        Default("PollingMaximumAttempts", "12");
        Default("PayerMessage", "eMilimo Digital Giving");
        Default("PayeeNote", "eMilimo contribution");
        return values;

        void Default(string key, string value)
        {
            if (!values.TryGetValue(key, out var current) || string.IsNullOrWhiteSpace(current)) values[key] = value;
        }
    }
}
