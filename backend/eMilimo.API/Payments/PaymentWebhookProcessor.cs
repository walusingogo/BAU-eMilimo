using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed class PaymentWebhookProcessor : IPaymentWebhookProcessor
{
    private readonly AppDbContext _db;
    private readonly IPaymentProviderRegistry _providers;
    private readonly IPaymentProviderConfigurationStore _providerConfigurations;
    private readonly IDataProtector _payloadProtector;
    private readonly IDataProtector _payerProtector;
    private readonly IDataProtector _statusTokenProtector;
    private readonly IEmailService _email;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PaymentWebhookProcessor> _logger;
    private readonly IPaymentFinalizationService _finalization;

    public PaymentWebhookProcessor(
        AppDbContext db,
        IPaymentProviderRegistry providers,
        IPaymentProviderConfigurationStore providerConfigurations,
        IDataProtectionProvider dataProtection,
        IEmailService email,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<PaymentWebhookProcessor> logger,
        IPaymentFinalizationService finalization)
    {
        _db = db;
        _providers = providers;
        _providerConfigurations = providerConfigurations;
        _payloadProtector = dataProtection.CreateProtector("eMilimo.PaymentWebhook.Payload.v1");
        _payerProtector = dataProtection.CreateProtector("eMilimo.DigitalGiving.PayerData.v1");
        _statusTokenProtector = dataProtection.CreateProtector("eMilimo.DigitalGiving.PublicStatusToken.v1");
        _email = email;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _finalization = finalization;
    }

    public async Task<PaymentWebhookProcessingResult> ProcessAsync(
        string providerKey,
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var normalizedProviderKey = providerKey.Trim().ToUpperInvariant();
        var configuration = await ResolveConfigurationAsync(normalizedProviderKey, rawBody, cancellationToken);
        var provider = _providers.GetRequired(configuration.ProviderKey);
        var settings = await _providerConfigurations.GetRuntimeSettingsAsync(configuration.Id, false, cancellationToken);
        var parsed = await provider.VerifyAndParseWebhookAsync(
            new PaymentWebhookRequest(rawBody, headers, settings), cancellationToken);
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
        var eventKey = parsed.IsAuthentic ? parsed.EventKey : $"INVALID-{payloadHash}";

        var webhookEvent = await ReceiveWebhookAsync(
            configuration.Id,
            eventKey,
            payloadHash,
            parsed.IsAuthentic,
            rawBody,
            correlationId,
            cancellationToken);
        if (webhookEvent.ProcessingStatus is "PROCESSED" or "REJECTED")
        {
            return new PaymentWebhookProcessingResult(
                Accepted: webhookEvent.SignatureValid,
                Duplicate: true,
                webhookEvent.ProcessingStatus,
                null,
                webhookEvent.SignatureValid ? "Webhook was already processed." : "Webhook authentication failed.");
        }

        if (!parsed.IsAuthentic)
        {
            webhookEvent.ProcessingStatus = "REJECTED";
            webhookEvent.FailureReason = Truncate(parsed.FailureReason ?? "Webhook authentication failed.", 1000);
            webhookEvent.ProcessedAtUtc = DateTime.UtcNow;
            webhookEvent.UpdatedDate = DateTime.UtcNow;
            webhookEvent.Version++;
            await _db.SaveChangesAsync(cancellationToken);
            return new PaymentWebhookProcessingResult(false, false, "REJECTED", null, "Webhook authentication failed.");
        }

        if (webhookEvent.ProcessingStatus == "PROCESSING" && webhookEvent.UpdatedDate > DateTime.UtcNow.AddMinutes(-2))
            return new PaymentWebhookProcessingResult(true, true, "PROCESSING", null, "Webhook processing is already in progress.");

        webhookEvent.ProcessingStatus = "PROCESSING";
        webhookEvent.ProcessingAttempts++;
        webhookEvent.FailureReason = null;
        webhookEvent.UpdatedDate = DateTime.UtcNow;
        webhookEvent.Version++;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await ProcessVerifiedWebhookAsync(
                webhookEvent.Id,
                configuration.Id,
                parsed,
                correlationId,
                cancellationToken);
            if (!result.Duplicate && result.ProcessingStatus == "PROCESSED" && result.PaymentReference is not null)
                await SendReceiptNotificationAsync(result.PaymentReference, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verified payment webhook {WebhookEventId} failed during processing.", webhookEvent.Id);
            _db.ChangeTracker.Clear();
            var failedEvent = await _db.PaymentWebhookEvents.SingleAsync(x => x.Id == webhookEvent.Id, cancellationToken);
            failedEvent.ProcessingStatus = "FAILED";
            failedEvent.FailureReason = Truncate(ex.Message, 1000);
            failedEvent.UpdatedDate = DateTime.UtcNow;
            failedEvent.Version++;
            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task<PaymentProviderConfiguration> ResolveConfigurationAsync(
        string providerKey,
        string rawBody,
        CancellationToken cancellationToken)
    {
        Guid? transactionId = null;
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            if (document.RootElement.TryGetProperty("externalId", out var value) &&
                value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var parsed))
                transactionId = parsed;
        }
        catch (JsonException) { }

        if (transactionId.HasValue)
        {
            var matched = await _db.DigitalCollections.AsNoTracking()
                .Where(x => x.Id == transactionId.Value && x.PaymentProviderConfiguration != null &&
                            x.PaymentProviderConfiguration.ProviderKey == providerKey)
                .Select(x => x.PaymentProviderConfiguration!)
                .SingleOrDefaultAsync(cancellationToken);
            if (matched is not null) return matched;
        }

        var enabled = await _db.PaymentProviderConfigurations.AsNoTracking()
            .Where(x => x.ProviderKey == providerKey && x.IsEnabled)
            .OrderByDescending(x => x.UpdatedDate)
            .ToListAsync(cancellationToken);
        return enabled.Count == 1
            ? enabled[0]
            : throw new KeyNotFoundException("Payment provider webhook configuration could not be resolved safely.");
    }

    private async Task<PaymentWebhookProcessingResult> ProcessVerifiedWebhookAsync(
        Guid webhookEventId,
        Guid providerConfigurationId,
        PaymentWebhookResult parsed,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var webhookEvent = await _db.PaymentWebhookEvents.SingleAsync(x => x.Id == webhookEventId, cancellationToken);
        var payment = string.IsNullOrWhiteSpace(parsed.ProviderTransactionId)
            ? null
            : await _db.DigitalCollections
            .SingleOrDefaultAsync(x =>
                x.PaymentProviderConfigurationId == providerConfigurationId &&
                x.ProviderTransactionId == parsed.ProviderTransactionId,
                cancellationToken);
        if (payment is null)
        {
            webhookEvent.ProcessingStatus = "UNMATCHED";
            webhookEvent.FailureReason = "No payment transaction matches the authenticated provider reference.";
            webhookEvent.ProcessedAtUtc = DateTime.UtcNow;
            webhookEvent.UpdatedDate = DateTime.UtcNow;
            webhookEvent.Version++;
            await _db.SaveChangesAsync(cancellationToken);
            return new PaymentWebhookProcessingResult(true, false, "UNMATCHED", null, "Webhook accepted for reconciliation.");
        }
        var result = await _finalization.ApplyVerifiedStatusAsync(payment.Id, new ProviderVerifiedPayment(
            parsed.ProviderTransactionId!, parsed.Status, parsed.Amount, parsed.CurrencyCode, parsed.ProviderFee,
            parsed.NetSettlementAmount, parsed.ProviderCompletedAtUtc, parsed.FailureReason),
            "PROVIDER_WEBHOOK", correlationId, cancellationToken: cancellationToken);
        _db.ChangeTracker.Clear();
        webhookEvent = await _db.PaymentWebhookEvents.SingleAsync(x => x.Id == webhookEventId, cancellationToken);
        webhookEvent.DigitalCollectionId = payment.Id;
        webhookEvent.ProcessingStatus = "PROCESSED";
        webhookEvent.ProcessedAtUtc = DateTime.UtcNow;
        webhookEvent.UpdatedDate = DateTime.UtcNow;
        webhookEvent.Version++;
        await _db.SaveChangesAsync(cancellationToken);
        return new PaymentWebhookProcessingResult(
            true,
            result.AlreadyCompleted,
            "PROCESSED",
            result.PaymentReference,
            result.Message);
    }

    private async Task<PaymentWebhookEvent> ReceiveWebhookAsync(
        Guid providerConfigurationId,
        string eventKey,
        string payloadHash,
        bool signatureValid,
        string rawBody,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var existing = await _db.PaymentWebhookEvents.SingleOrDefaultAsync(x =>
            x.PaymentProviderConfigurationId == providerConfigurationId && x.EventKey == eventKey, cancellationToken);
        if (existing is not null) return existing;

        var now = DateTime.UtcNow;
        var entity = new PaymentWebhookEvent
        {
            Id = Guid.NewGuid(),
            PaymentProviderConfigurationId = providerConfigurationId,
            EventKey = eventKey,
            PayloadHash = payloadHash,
            SignatureValid = signatureValid,
            ProcessingStatus = "RECEIVED",
            CorrelationId = correlationId,
            ProtectedPayload = _payloadProtector.Protect(rawBody),
            ReceivedAtUtc = now,
            CreatedDate = now,
            UpdatedDate = now,
            Version = 1
        };
        _db.PaymentWebhookEvents.Add(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (DbUpdateException)
        {
            _db.Entry(entity).State = EntityState.Detached;
            return await _db.PaymentWebhookEvents.SingleAsync(x =>
                x.PaymentProviderConfigurationId == providerConfigurationId && x.EventKey == eventKey, cancellationToken);
        }
    }

    private static string? Truncate(string? value, int maximumLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Length <= maximumLength ? value : value[..maximumLength];

    public async Task<bool> SendReceiptNotificationAsync(string paymentReference, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _db.DigitalCollections.AsNoTracking()
                .Include(x => x.Member)
                .Include(x => x.Lines).ThenInclude(x => x.ContributionType)
                .SingleOrDefaultAsync(x => x.PaymentReference == paymentReference && x.Status == PaymentTransactionStatuses.Succeeded, cancellationToken);
            if (payment is null) return false;
            PayerData? payer = null;
            if (!string.IsNullOrWhiteSpace(payment.ProtectedPayerData))
                payer = JsonSerializer.Deserialize<PayerData>(_payerProtector.Unprotect(payment.ProtectedPayerData));
            var email = payment.Member?.Email ?? payer?.Email;
            if (string.IsNullOrWhiteSpace(email)) return false;
            var name = payment.GiverType == DigitalGiverTypes.Anonymous
                ? "Contributor"
                : payment.Member is not null ? $"{payment.Member.FirstName} {payment.Member.LastName}" : payment.ContributorNameSnapshot ?? "Contributor";
            var configuredFrontend = await _db.SystemConfigs.AsNoTracking()
                .Where(x => !x.IsArchived && x.ConfigKey == "giving_base_url")
                .Select(x => x.ConfigValue)
                .FirstOrDefaultAsync(cancellationToken);
            var frontend = (_configuration["Giving:PublicUrl"] ?? configuredFrontend ?? "http://localhost:9200").Trim().TrimEnd('/');
            if (!_configuration.GetValue<bool>("DigitalGiving:AllowLoopbackPublicUrls", false)
                && Uri.TryCreate(frontend, UriKind.Absolute, out var publicUri)
                && publicUri.IsLoopback
                && _environment.IsProduction())
                throw new InvalidOperationException("A non-loopback giving_base_url must be configured before production receipt links can be sent.");
            var token = _statusTokenProtector.Unprotect(payment.ProtectedPublicStatusToken);
            var receiptUrl = $"{frontend}/receipt/{Uri.EscapeDataString(payment.PaymentReference)}?token={Uri.EscapeDataString(token)}";
            var lines = string.Join("", payment.Lines.OrderBy(x => x.CreatedDate).Select(x =>
                $"<tr><td style=\"padding:8px;border-bottom:1px solid #eee\">{WebUtility.HtmlEncode(x.ContributionType?.Name ?? "Contribution")}</td><td style=\"padding:8px;border-bottom:1px solid #eee;text-align:right\">{payment.CurrencyCode} {x.Amount:N2}</td></tr>"));
            var body = $"""
                <div style="font-family:Segoe UI,Arial,sans-serif;color:#241c15;max-width:620px;margin:auto">
                  <div style="border-top:5px solid #b8862e;padding:28px;border-radius:8px;background:#fff">
                    <p style="font-size:12px;letter-spacing:.12em;color:#8a7f6a">DIGITAL GIVING</p>
                    <h2 style="color:#173e55">Thank you, {WebUtility.HtmlEncode(name)}</h2>
                    <p>Your payment has been verified and the contribution has been posted.</p>
                    <div style="padding:18px;background:#f7f4ec;border-radius:10px;margin:20px 0"><strong style="font-size:24px">{payment.CurrencyCode} {payment.GrossAmount:N2}</strong><br><span>{WebUtility.HtmlEncode(payment.PaymentReference)}</span></div>
                    <table style="width:100%;border-collapse:collapse">{lines}</table>
                    <p style="margin-top:24px"><a href="{WebUtility.HtmlEncode(receiptUrl)}" style="display:inline-block;background:#173e55;color:#fff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:700">View or download receipt</a></p>
                    <p style="font-size:12px;color:#64748b;margin-top:26px">This receipt is generated only after provider verification. Powered by BAU Solutions Limited.</p>
                  </div>
                </div>
                """;
            await _email.SendAsync(email.Trim(), $"Digital Giving receipt {payment.PaymentReference}", body, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            // Notification delivery and receipt-link preparation must never roll
            // back or relabel an already verified financial transaction.
            _logger.LogError(ex, "Digital Giving receipt notification could not be prepared for {PaymentReference}.", paymentReference);
            return false;
        }
    }

    private sealed record PayerData(string PhoneNumber, string? Email, string? GuestName);
}
