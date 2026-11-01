using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Services;

public static class CommunicationProviders
{
    public const string Twilio = "Twilio";
    public const string Brevo = "Brevo";
    public const string Independent = "Independent";

    public static readonly IReadOnlyList<CommunicationProviderOption> Catalog =
    [
        new(Twilio, "Twilio", ["sms", "whatsapp"]),
        new(Brevo, "Brevo", ["sms", "whatsapp"]),
        new(Independent, "Independent REST endpoint", ["sms", "whatsapp"])
    ];

    public static bool IsSupported(string? provider) => Catalog.Any(x => x.Key.Equals(provider, StringComparison.OrdinalIgnoreCase));
}

public sealed record CommunicationProviderOption(string Key, string Name, IReadOnlyList<string> Channels);

public sealed record CommunicationChannelSettings
{
    public bool Enabled { get; init; }
    public string Provider { get; init; } = CommunicationProviders.Brevo;
    public string AccountId { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public string Sender { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string AuthHeaderName { get; init; } = "Authorization";
    public bool HasSecret { get; init; }
}

public sealed record CommunicationSettings
{
    public CommunicationChannelSettings Sms { get; init; } = new();
    public CommunicationChannelSettings WhatsApp { get; init; } = new();
    public int TimeoutMilliseconds { get; init; } = 15000;
}

public interface ICommunicationSettingsProvider
{
    Task<CommunicationSettings> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CommunicationSettings settings, CancellationToken cancellationToken = default);
}

public sealed class CommunicationSettingsProvider(IServiceScopeFactory scopes, IDataProtectionProvider protection) : ICommunicationSettingsProvider
{
    private readonly IDataProtector _protector = protection.CreateProtector("eMilimo.CommunicationSettings.Secret.v1");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public async Task<CommunicationSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.IntegrationSettings.AsNoTracking().FirstOrDefaultAsync(x => x.IntegrationKey == "communications", cancellationToken);
        return WithSecretIndicators(ReadStored(row));
    }

    public async Task SaveAsync(CommunicationSettings settings, CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.IntegrationSettings.FirstOrDefaultAsync(x => x.IntegrationKey == "communications", cancellationToken);
        var previous = ReadStored(row);
        var normalized = Normalize(settings);
        var stored = normalized with
        {
            Sms = normalized.Sms with { Secret = RetainedSecret(normalized.Sms, previous.Sms), HasSecret = false },
            WhatsApp = normalized.WhatsApp with { Secret = RetainedSecret(normalized.WhatsApp, previous.WhatsApp), HasSecret = false }
        };
        var payload = _protector.Protect(JsonSerializer.Serialize(stored, JsonOptions));
        if (row is null)
        {
            db.IntegrationSettings.Add(new IntegrationSetting { Id = Guid.NewGuid(), IntegrationKey = "communications", ProtectedSettings = payload, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1 });
        }
        else
        {
            row.ProtectedSettings = payload;
            row.UpdatedDate = DateTime.UtcNow;
            row.Version++;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private CommunicationSettings ReadStored(IntegrationSetting? row)
    {
        if (row is null) return new CommunicationSettings();
        try
        {
            var json = _protector.Unprotect(row.ProtectedSettings);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("Sms", out _) || document.RootElement.TryGetProperty("sms", out _))
                return Normalize(JsonSerializer.Deserialize<CommunicationSettings>(json, JsonOptions) ?? new CommunicationSettings());
            var legacy = JsonSerializer.Deserialize<LegacyCommunicationSettings>(json, JsonOptions) ?? new LegacyCommunicationSettings();
            return Normalize(new CommunicationSettings
            {
                Sms = new() { Enabled = legacy.SmsEnabled, Provider = legacy.Provider, AccountId = legacy.AccountSid, Secret = legacy.AuthToken, Sender = legacy.SmsFrom },
                WhatsApp = new() { Enabled = legacy.WhatsAppEnabled, Provider = legacy.Provider, AccountId = legacy.AccountSid, Secret = legacy.AuthToken, Sender = legacy.WhatsAppFrom },
                TimeoutMilliseconds = legacy.TimeoutMilliseconds
            });
        }
        catch { return new CommunicationSettings(); }
    }

    private static CommunicationSettings Normalize(CommunicationSettings value) => value with
    {
        Sms = NormalizeChannel(value.Sms),
        WhatsApp = NormalizeChannel(value.WhatsApp),
        TimeoutMilliseconds = Math.Clamp(value.TimeoutMilliseconds, 5000, 120000)
    };

    private static CommunicationChannelSettings NormalizeChannel(CommunicationChannelSettings? value)
    {
        value ??= new CommunicationChannelSettings();
        return value with
        {
            Provider = string.IsNullOrWhiteSpace(value.Provider) ? CommunicationProviders.Brevo : value.Provider.Trim(),
            AccountId = value.AccountId?.Trim() ?? string.Empty,
            Secret = value.Secret?.Trim() ?? string.Empty,
            Sender = value.Sender?.Trim() ?? string.Empty,
            Endpoint = value.Endpoint?.Trim() ?? string.Empty,
            AuthHeaderName = string.IsNullOrWhiteSpace(value.AuthHeaderName) ? "Authorization" : value.AuthHeaderName.Trim()
        };
    }

    private static string RetainedSecret(CommunicationChannelSettings incoming, CommunicationChannelSettings previous) =>
        !string.IsNullOrWhiteSpace(incoming.Secret) ? incoming.Secret :
        incoming.Provider.Equals(previous.Provider, StringComparison.OrdinalIgnoreCase) ? previous.Secret : string.Empty;

    private static CommunicationSettings WithSecretIndicators(CommunicationSettings value) => value with
    {
        Sms = value.Sms with { HasSecret = !string.IsNullOrWhiteSpace(value.Sms.Secret) },
        WhatsApp = value.WhatsApp with { HasSecret = !string.IsNullOrWhiteSpace(value.WhatsApp.Secret) }
    };

    private sealed record LegacyCommunicationSettings(bool SmsEnabled = false, bool WhatsAppEnabled = false, string Provider = CommunicationProviders.Twilio, string AccountSid = "", string AuthToken = "", string SmsFrom = "", string WhatsAppFrom = "", int TimeoutMilliseconds = 15000, bool HasAuthToken = false);
}

public interface ICommunicationService
{
    Task SendSmsAsync(string recipient, string message, CancellationToken cancellationToken = default, string action = "General", Guid? retryOfCommunicationLogId = null);
    Task SendWhatsAppAsync(string recipient, string message, CancellationToken cancellationToken = default, string action = "General", Guid? retryOfCommunicationLogId = null);
}

public sealed partial class CommunicationService(ICommunicationSettingsProvider settingsProvider, IHttpClientFactory clients, ISystemLogWriter logs,
    ILogger<CommunicationService> logger, AppDbContext db, IDataProtectionProvider protection) : ICommunicationService
{
    private readonly IDataProtector _messageProtector = protection.CreateProtector("eMilimo.CommunicationLog.RetryPayload.v1");

    public async Task SendSmsAsync(string recipient, string message, CancellationToken cancellationToken = default, string action = "General", Guid? retryOfCommunicationLogId = null)
    {
        var settings = await settingsProvider.GetAsync(cancellationToken);
        if (!settings.Sms.Enabled) await Disabled("SMS", recipient, action, message, retryOfCommunicationLogId, cancellationToken);
        await Send(settings.Sms, settings.TimeoutMilliseconds, "SMS", recipient, message, action, retryOfCommunicationLogId, cancellationToken);
    }

    public async Task SendWhatsAppAsync(string recipient, string message, CancellationToken cancellationToken = default, string action = "General", Guid? retryOfCommunicationLogId = null)
    {
        var settings = await settingsProvider.GetAsync(cancellationToken);
        if (!settings.WhatsApp.Enabled) await Disabled("WhatsApp", recipient, action, message, retryOfCommunicationLogId, cancellationToken);
        await Send(settings.WhatsApp, settings.TimeoutMilliseconds, "WhatsApp", recipient, message, action, retryOfCommunicationLogId, cancellationToken);
    }

    private async Task Disabled(string channel, string recipient, string action, string message, Guid? retryOf, CancellationToken token)
    {
        await RecordAsync(channel, "Not configured", recipient, action, "Failed", null, $"{channel} notifications are disabled.", message, retryOf, token);
        throw new InvalidOperationException($"{channel} notifications are disabled in System Settings.");
    }

    private async Task Send(CommunicationChannelSettings settings, int timeout, string channel, string recipient, string message, string action, Guid? retryOf, CancellationToken token)
    {
        try
        {
            Validate(settings, channel);
            var providerId = settings.Provider.ToUpperInvariant() switch
            {
                "TWILIO" => await SendTwilio(settings, timeout, channel, recipient, message, token),
                "BREVO" => await SendBrevo(settings, timeout, channel, recipient, message, token),
                "INDEPENDENT" => await SendIndependent(settings, timeout, channel, recipient, message, token),
                _ => throw new InvalidOperationException("The selected communication provider is not supported.")
            };
            await RecordAsync(channel, settings.Provider, recipient, action, "Success", providerId, null, message, retryOf, token);
            await logs.WriteAsync(new SystemLog { Level = "Information", Category = "Communication", Process = "Notifications", Action = action, Message = $"{channel} notification was accepted by {settings.Provider} for {Mask(recipient)}." }, token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Channel} delivery through {Provider} failed.", channel, settings.Provider);
            await RecordAsync(channel, settings.Provider, recipient, action, "Failed", null, ex.Message, message, retryOf, token);
            await logs.WriteAsync(new SystemLog { Level = "Error", Category = "Communication", Process = "Notifications", Action = action, Message = $"{channel} notification through {settings.Provider} failed for {Mask(recipient)}.", Exception = ex.Message }, token);
            throw;
        }
    }

    private async Task<string?> SendTwilio(CommunicationChannelSettings settings, int timeout, string channel, string recipient, string message, CancellationToken token)
    {
        var to = channel == "WhatsApp" ? PrefixWhatsApp(recipient) : recipient;
        var from = channel == "WhatsApp" ? PrefixWhatsApp(settings.Sender) : settings.Sender;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(settings.AccountId)}/Messages.json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.AccountId}:{settings.Secret}")));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["To"] = to, ["From"] = from, ["Body"] = message });
        var body = await SendRequest(request, timeout, settings.Provider, token);
        return JsonValue(body, "sid");
    }

    private async Task<string?> SendBrevo(CommunicationChannelSettings settings, int timeout, string channel, string recipient, string message, CancellationToken token)
    {
        var url = channel == "SMS" ? "https://api.brevo.com/v3/transactionalSMS/send" : "https://api.brevo.com/v3/whatsapp/sendMessage";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("api-key", settings.Secret);
        request.Headers.Accept.ParseAdd("application/json");
        request.Content = channel == "SMS"
            ? JsonContent.Create(new { sender = settings.Sender, recipient, content = message, type = "transactional", unicodeEnabled = true })
            : JsonContent.Create(new { senderNumber = Digits(settings.Sender), contactNumbers = new[] { Digits(recipient) }, text = message });
        var body = await SendRequest(request, timeout, settings.Provider, token);
        return JsonValue(body, "messageId");
    }

    private async Task<string?> SendIndependent(CommunicationChannelSettings settings, int timeout, string channel, string recipient, string message, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, settings.Endpoint);
        if (!string.IsNullOrWhiteSpace(settings.Secret)) request.Headers.TryAddWithoutValidation(settings.AuthHeaderName, settings.Secret);
        request.Headers.Accept.ParseAdd("application/json");
        request.Content = JsonContent.Create(new { channel = channel.ToLowerInvariant(), to = recipient, from = settings.Sender, message });
        var body = await SendRequest(request, timeout, settings.Provider, token);
        return JsonValue(body, "messageId") ?? JsonValue(body, "id");
    }

    private async Task<string> SendRequest(HttpRequestMessage request, int timeout, string provider, CancellationToken token)
    {
        var client = clients.CreateClient("communications");
        client.Timeout = TimeSpan.FromMilliseconds(Math.Clamp(timeout, 5000, 120000));
        using var response = await client.SendAsync(request, token);
        var body = await response.Content.ReadAsStringAsync(token);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(ProviderFailure(provider, response, body));
        return body;
    }

    public static string? ValidateSettings(CommunicationChannelSettings settings, string channel, bool secretAlreadySaved)
    {
        if (!CommunicationProviders.IsSupported(settings.Provider)) return "Select a supported provider.";
        if (!settings.Enabled) return null;
        if (string.IsNullOrWhiteSpace(settings.Sender)) return $"Enter the {channel} sender.";
        if (string.IsNullOrWhiteSpace(settings.Secret) && !secretAlreadySaved) return $"Enter the {settings.Provider} credential for {channel}.";
        if (settings.Provider.Equals(CommunicationProviders.Twilio, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(settings.AccountId)) return "Enter the Twilio Account SID.";
        if (settings.Provider.Equals(CommunicationProviders.Independent, StringComparison.OrdinalIgnoreCase))
        {
            if (!ValidEndpoint(settings.Endpoint)) return "Enter an HTTPS provider endpoint. HTTP is accepted only for a loopback development endpoint.";
            if (!HeaderName().IsMatch(settings.AuthHeaderName)) return "Enter a valid authentication header name.";
        }
        return null;
    }

    private static void Validate(CommunicationChannelSettings settings, string channel)
    {
        var error = ValidateSettings(settings, channel, !string.IsNullOrWhiteSpace(settings.Secret));
        if (error is not null) throw new InvalidOperationException(error);
    }

    private static bool ValidEndpoint(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
    }

    private static string ProviderFailure(string provider, HttpResponseMessage response, string body)
    {
        const int maxLength = 400;
        var reason = body;
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            reason = root.TryGetProperty("message", out var message) ? message.ToString() : root.TryGetProperty("detail", out var detail) ? detail.ToString() : body;
        }
        catch (JsonException) { }
        reason = new string((reason ?? string.Empty).Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (reason.Length > maxLength) reason = reason[..maxLength];
        return $"{provider} returned HTTP {(int)response.StatusCode}{(string.IsNullOrWhiteSpace(reason) ? "." : $": {reason}")}";
    }

    private static string? JsonValue(string body, string property)
    {
        try { using var json = JsonDocument.Parse(body); return json.RootElement.TryGetProperty(property, out var value) ? value.ToString() : null; }
        catch (JsonException) { return null; }
    }

    private static string PrefixWhatsApp(string value) => value.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase) ? value : $"whatsapp:{value}";
    private static string Digits(string value) => new(value.Where(char.IsDigit).ToArray());
    private static string Mask(string value) { var clean = value.Replace("whatsapp:", "", StringComparison.OrdinalIgnoreCase); return clean.Length <= 4 ? "****" : $"***{clean[^4..]}"; }

    private async Task RecordAsync(string channel, string provider, string recipient, string action, string status, string? providerId, string? failure, string message, Guid? retryOf, CancellationToken token)
    {
        db.CommunicationLogs.Add(new CommunicationLog { Channel = channel, Provider = provider, Recipient = recipient, Action = action, Status = status, ProviderMessageId = providerId, FailureReason = failure is { Length: > 1000 } ? failure[..1000] : failure, ProtectedMessage = _messageProtector.Protect(message), RetryOfCommunicationLogId = retryOf });
        await db.SaveChangesAsync(token);
    }

    [GeneratedRegex("^[A-Za-z0-9-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex HeaderName();
}
