using System.Text.Json;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed record SandboxPaymentTestRequest(decimal Amount, string Msisdn);

public sealed class PaymentDeveloperService(
    AppDbContext db,
    IPaymentProviderRegistry providers,
    IPaymentProviderConfigurationStore configurations,
    IDataProtectionProvider dataProtection)
{
    private readonly IDataProtector _protector = dataProtection.CreateProtector("eMilimo.PaymentProvider.Configuration.v1");

    public async Task<PaymentProviderConnectionTestResult> TestConnectionAsync(Guid configurationId, Guid actor,
        CancellationToken cancellationToken)
    {
        var entity = await db.PaymentProviderConfigurations.SingleOrDefaultAsync(x => x.Id == configurationId, cancellationToken)
            ?? throw new KeyNotFoundException("Payment provider configuration not found.");
        var provider = providers.GetRequired(entity.ProviderKey);
        if (provider is not IPaymentProviderDiagnostics diagnostics)
            throw new InvalidOperationException("This provider does not expose a connection test capability.");
        var settings = await configurations.GetRuntimeSettingsAsync(entity.Id, false, cancellationToken);
        var result = await diagnostics.TestConnectionAsync(settings, cancellationToken);
        entity.IsValidated = result.Succeeded;
        entity.LastValidatedAtUtc = DateTime.UtcNow;
        entity.UpdatedById = actor;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.Version++;
        var version = await db.IntegrationConfigurationAudits
            .SingleOrDefaultAsync(x => x.IntegrationConfigurationId == entity.Id &&
                                       x.ConfigurationVersion == entity.ConfigurationVersion, cancellationToken);
        if (version is not null)
        {
            version.ValidationStatus = result.Succeeded ? "VALID" : "INVALID";
            version.ValidationMessage = Truncate(result.Message, 1000);
            version.ValidatedAtUtc = DateTime.UtcNow;
            version.UpdatedById = actor;
            version.UpdatedDate = DateTime.UtcNow;
            version.Version++;
        }
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<object> SandboxPaymentAsync(Guid configurationId, SandboxPaymentTestRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await db.PaymentProviderConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == configurationId, cancellationToken)
            ?? throw new KeyNotFoundException("Payment provider configuration not found.");
        if (!entity.Environment.Equals("SANDBOX", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Developer Mode can initiate test payments only through a sandbox profile.");
        if (!entity.IsValidated)
            throw new InvalidOperationException("Validate the sandbox connection before initiating a test payment.");
        if (request.Amount <= 0 || request.Amount > 1000)
            throw new InvalidOperationException("Sandbox test amount must be between EUR 0.01 and EUR 1,000.00.");
        var settings = await configurations.GetRuntimeSettingsAsync(entity.Id, false, cancellationToken);
        var provider = providers.GetRequired(entity.ProviderKey);
        var paymentId = Guid.NewGuid();
        var callbackHost = settings.GetValueOrDefault("CallbackHost");
        var callback = string.IsNullOrWhiteSpace(callbackHost)
            ? new Uri("http://localhost/api/public/digital-giving/webhooks/MTN_MOMO_DIRECT")
            : new Uri($"https://{callbackHost}/api/public/digital-giving/webhooks/MTN_MOMO_DIRECT");
        var result = await provider.InitiateAsync(new PaymentInitiationRequest(
            paymentId, $"SANDBOX-{paymentId:N}", "MTN_MOMO", request.Amount, "EUR", request.Msisdn,
            callback, $"developer-{paymentId:N}", settings), cancellationToken);
        return new
        {
            paymentReference = paymentId,
            result.ProviderTransactionId,
            result.Status,
            accepted = result.Status == PaymentTransactionStatuses.Pending,
            result.ProviderMessage,
            ledgerPosting = "DISABLED_FOR_DEVELOPER_SANDBOX_TEST"
        };
    }

    public async Task<IReadOnlyList<object>> HistoryAsync(Guid configurationId, CancellationToken cancellationToken) =>
        await db.IntegrationConfigurationAudits.AsNoTracking()
            .Where(x => x.IntegrationConfigurationId == configurationId)
            .OrderByDescending(x => x.ConfigurationVersion)
            .Select(x => (object)new
            {
                x.Id, x.ConfigurationVersion, x.SanitizedConfigurationSnapshot, x.ChangedFieldsJson,
                x.ValidationStatus, x.ValidationMessage, x.ValidatedAtUtc,
                changedBy = x.CreatedById, changedAtUtc = x.CreatedDate, x.ChangeReason
            }).ToListAsync(cancellationToken);

    public async Task<object> RestoreAsync(Guid configurationId, int versionNumber, string reason, Guid actor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("A restoration reason is required.");
        var entity = await db.PaymentProviderConfigurations.SingleOrDefaultAsync(x => x.Id == configurationId, cancellationToken)
            ?? throw new KeyNotFoundException("Payment provider configuration not found.");
        var source = await db.IntegrationConfigurationAudits.AsNoTracking().SingleOrDefaultAsync(x =>
            x.IntegrationConfigurationId == configurationId && x.ConfigurationVersion == versionNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Configuration version not found.");
        if (source.ValidationStatus != "VALID")
            throw new InvalidOperationException("Only a previously validated configuration version can be restored.");

        // Verify that the historical secret snapshot is still decryptable before replacing the active profile.
        _ = _protector.Unprotect(source.ProtectedConfigurationSnapshot);
        entity.ConfigurationVersion++;
        entity.ProtectedConfiguration = source.ProtectedConfigurationSnapshot;
        entity.IsValidated = true;
        entity.LastValidatedAtUtc = source.ValidatedAtUtc;
        entity.UpdatedById = actor;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.Version++;
        db.IntegrationConfigurationAudits.Add(new IntegrationConfigurationAud
        {
            Id = Guid.NewGuid(), IntegrationConfigurationId = entity.Id, Action = "RESTORED",
            ConfigurationVersion = entity.ConfigurationVersion,
            ProtectedConfigurationSnapshot = source.ProtectedConfigurationSnapshot,
            SanitizedConfigurationSnapshot = source.SanitizedConfigurationSnapshot,
            ChangedFieldsJson = JsonSerializer.Serialize(new[] { $"RESTORED_FROM_VERSION_{versionNumber}" }),
            ValidationStatus = "VALID", ValidationMessage = $"Restored from validated version {versionNumber}.",
            ValidatedAtUtc = source.ValidatedAtUtc, ChangeReason = Truncate(reason.Trim(), 500),
            CreatedById = actor, UpdatedById = actor, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1
        });
        await db.SaveChangesAsync(cancellationToken);
        return new { entity.Id, entity.ConfigurationVersion, entity.IsValidated, message = "Validated configuration restored." };
    }

    private static string Truncate(string value, int maximum) => value.Length <= maximum ? value : value[..maximum];
}
