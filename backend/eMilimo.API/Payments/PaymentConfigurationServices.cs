using System.Text.Json;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed class PaymentProviderConfigurationStore : IPaymentProviderConfigurationStore
{
    private static readonly HashSet<string> VisibleSettingKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "ApiBaseUrl",
        "TokenEndpointPath",
        "RequestToPayEndpointPath",
        "StatusEndpointTemplate",
        "BalanceEndpointPath",
        "AccountHolderEndpointTemplate",
        "ApprovedApiHost",
        "ApiVersion",
        "TargetEnvironment",
        "ApiUser",
        "SupportedCurrencies",
        "MsisdnCountryCode",
        "MsisdnNationalLength",
        "MsisdnAllowedPrefixes",
        "TimeoutSeconds",
        "PollingInitialSeconds",
        "PollingMaximumSeconds",
        "PollingMaximumAttempts",
        "CallbackHost",
        "CallbackUrl",
        "PayerMessage",
        "PayeeNote"
    };

    private readonly AppDbContext _db;
    private readonly IPaymentProviderRegistry _registry;
    private readonly IHostEnvironment _environment;
    private readonly IDataProtector _protector;
    private readonly PaymentProviderEndpointPolicy _endpointPolicy;

    public PaymentProviderConfigurationStore(
        AppDbContext db,
        IPaymentProviderRegistry registry,
        IHostEnvironment environment,
        IDataProtectionProvider dataProtection,
        PaymentProviderEndpointPolicy endpointPolicy)
    {
        _db = db;
        _registry = registry;
        _environment = environment;
        _protector = dataProtection.CreateProtector("eMilimo.PaymentProvider.Configuration.v1");
        _endpointPolicy = endpointPolicy;
    }

    public async Task<IReadOnlyList<PaymentProviderConfigurationView>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var configurations = await _db.PaymentProviderConfigurations
            .AsNoTracking()
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
        return configurations.Select(ToView).ToArray();
    }

    public async Task<PaymentProviderConfigurationView> SaveAsync(
        PaymentProviderConfigurationDraft draft,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var providerKey = NormalizeKey(draft.ProviderKey);
        var provider = _registry.GetRequired(providerKey);
        ValidateActivation(draft, provider.Descriptor);

        var normalizedEnvironment = NormalizeKey(draft.Environment, "Environment", 30);
        var duplicate = await _db.PaymentProviderConfigurations.AnyAsync(x =>
            x.ProviderKey == providerKey && x.Environment == normalizedEnvironment &&
            (!draft.Id.HasValue || x.Id != draft.Id.Value), cancellationToken);
        if (duplicate)
            throw new InvalidOperationException($"A configuration for provider '{providerKey}' already exists.");

        var isNew = !draft.Id.HasValue;
        PaymentProviderConfiguration entity;
        Dictionary<string, string> storedSettings;
        if (draft.Id.HasValue)
        {
            entity = await _db.PaymentProviderConfigurations.SingleOrDefaultAsync(x => x.Id == draft.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Payment provider configuration not found.");
            storedSettings = new Dictionary<string, string>(Unprotect(entity.ProtectedConfiguration), StringComparer.OrdinalIgnoreCase);
            foreach (var setting in NormalizeSettings(draft.Settings).Where(x => !string.IsNullOrWhiteSpace(x.Value)))
                storedSettings[setting.Key] = setting.Value;
            entity.UpdatedById = actorUserId;
            entity.UpdatedDate = DateTime.UtcNow;
            entity.Version++;
            entity.ConfigurationVersion++;
        }
        else
        {
            entity = new PaymentProviderConfiguration
            {
                Id = Guid.NewGuid(),
                CreatedById = actorUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Version = 1
            };
            storedSettings = NormalizeSettings(draft.Settings);
            _db.PaymentProviderConfigurations.Add(entity);
        }

        // Older placeholder builds exposed a WebhookSecret field for MTN.
        // MTN Collections callbacks do not use that secret; callbacks are
        // verified by querying the authenticated provider status endpoint.
        if (providerKey == "MTN_MOMO_DIRECT")
        {
            storedSettings.Remove("WebhookSecret");
            storedSettings = MtnMomoProfile.ApplyDefaults(storedSettings, normalizedEnvironment);
        }

        ValidateSettings(providerKey, normalizedEnvironment, draft.IsEnabled, storedSettings);

        entity.ProviderKey = providerKey;
        entity.DisplayName = RequireText(draft.DisplayName, "Display name", 120);
        entity.ProviderKind = NormalizeKey(draft.ProviderKind, "Provider kind", 30);
        entity.Environment = normalizedEnvironment;
        entity.IsEnabled = draft.IsEnabled;
        entity.IsValidated = false;
        entity.LastValidatedAtUtc = null;
        entity.ProtectedConfiguration = _protector.Protect(JsonSerializer.Serialize(storedSettings));

        var sanitized = storedSettings
            .Where(x => VisibleSettingKeys.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        _db.IntegrationConfigurationAudits.Add(new IntegrationConfigurationAud
        {
            Id = Guid.NewGuid(),
            IntegrationConfigurationId = entity.Id,
            Action = isNew ? "CREATED" : "UPDATED",
            ConfigurationVersion = entity.ConfigurationVersion,
            ProtectedConfigurationSnapshot = entity.ProtectedConfiguration,
            SanitizedConfigurationSnapshot = JsonSerializer.Serialize(sanitized),
            ChangedFieldsJson = JsonSerializer.Serialize(storedSettings.Keys.OrderBy(x => x).ToArray()),
            ValidationStatus = "NOT_TESTED",
            CreatedById = actorUserId,
            UpdatedById = actorUserId,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Version = 1
        });

        await _db.SaveChangesAsync(cancellationToken);
        return ToView(entity);
    }

    private void ValidateSettings(
        string providerKey,
        string environment,
        bool isEnabled,
        IReadOnlyDictionary<string, string> settings)
    {
        if (providerKey == "DEVELOPMENT_SANDBOX")
        {
            if (isEnabled && (!settings.TryGetValue("WebhookSecret", out var webhookSecret) || webhookSecret.Length < 16))
                throw new InvalidOperationException("The development sandbox requires a webhook secret of at least 16 characters.");
            return;
        }

        if (providerKey != "MTN_MOMO_DIRECT")
            return;

        foreach (var required in new[] { "ApiBaseUrl", "TargetEnvironment" })
        {
            if (!settings.TryGetValue(required, out var value) || string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"MTN MoMo requires {required}.");
        }
        if (isEnabled)
            foreach (var required in new[] { "SubscriptionKey", "ApiUser", "ApiKey" })
                if (!settings.TryGetValue(required, out var value) || string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException($"MTN MoMo requires {required} before it can be enabled.");

        _endpointPolicy.ValidateBaseUri(providerKey, environment, settings["ApiBaseUrl"]);
        _endpointPolicy.ValidateRelativePath(settings["TokenEndpointPath"], "Token endpoint");
        _endpointPolicy.ValidateRelativePath(settings["RequestToPayEndpointPath"], "Request to Pay endpoint");
        _endpointPolicy.ValidateRelativePath(settings["StatusEndpointTemplate"], "Status endpoint", isTemplate: true);
        _endpointPolicy.ValidateRelativePath(settings["BalanceEndpointPath"], "Account balance endpoint");
        _endpointPolicy.ValidateRelativePath(settings["AccountHolderEndpointTemplate"], "Account holder endpoint", isTemplate: true);
        var approvedHost = settings.GetValueOrDefault("ApprovedApiHost");
        if (!string.IsNullOrWhiteSpace(approvedHost) &&
            !new Uri(settings["ApiBaseUrl"]).IdnHost.Equals(approvedHost, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Approved API Host must exactly match the validated API Base URL host.");
        if (settings.TryGetValue("ApiUser", out var apiUser) && !string.IsNullOrWhiteSpace(apiUser) && !Guid.TryParse(apiUser, out _))
            throw new InvalidOperationException("MTN MoMo API User must be a valid UUID.");
        if (string.Equals(environment.Trim(), "SANDBOX", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(settings["TargetEnvironment"], "sandbox", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("MTN MoMo sandbox configurations must use Target Environment 'sandbox'.");
        if (string.Equals(environment, "PRODUCTION", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(settings["TargetEnvironment"], "mtnzambia", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("MTN Zambia production configurations must use Target Environment 'mtnzambia'.");
        ValidateInteger(settings, "TimeoutSeconds", 5, 120);
        ValidateInteger(settings, "PollingInitialSeconds", 5, 3600);
        ValidateInteger(settings, "PollingMaximumSeconds", 10, 86400);
        ValidateInteger(settings, "PollingMaximumAttempts", 1, 100);
        if (settings.TryGetValue("PayerMessage", out var payerMessage) && payerMessage.Length > 160)
            throw new InvalidOperationException("MTN payer message cannot exceed 160 characters.");
        if (settings.TryGetValue("PayeeNote", out var payeeNote) && payeeNote.Length > 160)
            throw new InvalidOperationException("MTN payee note cannot exceed 160 characters.");
        if (settings.TryGetValue("CallbackHost", out var callbackHost) && !string.IsNullOrWhiteSpace(callbackHost) &&
            (callbackHost.Contains('/') || callbackHost.Contains(':') || !Uri.CheckHostName(callbackHost).Equals(UriHostNameType.Dns)))
            throw new InvalidOperationException("Callback Host must be a DNS hostname without a scheme, port or path.");
        if (settings.TryGetValue("CallbackUrl", out var callbackUrl) && !string.IsNullOrWhiteSpace(callbackUrl))
        {
            if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var parsedCallback))
                throw new InvalidOperationException("Callback URL must be an absolute HTTPS URL.");
            _endpointPolicy.ValidateCallback(parsedCallback, callbackHost, environment);
        }
    }

    private static void ValidateInteger(IReadOnlyDictionary<string, string> settings, string key, int minimum, int maximum)
    {
        if (!settings.TryGetValue(key, out var value) || !int.TryParse(value, out var parsed) || parsed < minimum || parsed > maximum)
            throw new InvalidOperationException($"{key} must be a whole number between {minimum} and {maximum}.");
    }

    public async Task<IReadOnlyDictionary<string, string>> GetRuntimeSettingsAsync(
        Guid providerConfigurationId,
        bool requireEnabled = true,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _db.PaymentProviderConfigurations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == providerConfigurationId, cancellationToken)
            ?? throw new KeyNotFoundException("Payment provider configuration not found.");
        if (requireEnabled && !configuration.IsEnabled)
            throw new InvalidOperationException("The payment provider configuration is disabled.");
        var result = new Dictionary<string, string>(Unprotect(configuration.ProtectedConfiguration), StringComparer.OrdinalIgnoreCase)
        {
            ["_ConfigurationId"] = configuration.Id.ToString("D"),
            ["_ConfigurationVersion"] = configuration.ConfigurationVersion.ToString(),
            ["_Environment"] = configuration.Environment
        };
        return result;
    }

    private PaymentProviderConfigurationView ToView(PaymentProviderConfiguration configuration)
    {
        var descriptor = _registry.GetRequired(configuration.ProviderKey).Descriptor;
        IReadOnlyDictionary<string, string> settings;
        try { settings = Unprotect(configuration.ProtectedConfiguration); }
        catch { settings = new Dictionary<string, string>(); }
        var keys = settings.Keys.OrderBy(x => x).ToArray();
        var visibleSettings = settings
            .Where(x => VisibleSettingKeys.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        return new PaymentProviderConfigurationView(
            configuration.Id,
            configuration.ProviderKey,
            configuration.DisplayName,
            configuration.ProviderKind,
            configuration.Environment,
            configuration.IsEnabled,
            descriptor.IsImplemented,
            descriptor.IsProductionReady,
            keys,
            visibleSettings,
            descriptor.UnavailableReason,
            configuration.ConfigurationVersion,
            configuration.LastValidatedAtUtc,
            configuration.IsValidated);
    }

    private void ValidateActivation(PaymentProviderConfigurationDraft draft, PaymentProviderDescriptor descriptor)
    {
        if (!draft.IsEnabled)
            return;
        if (!descriptor.IsImplemented)
            throw new InvalidOperationException(descriptor.UnavailableReason ?? "This payment provider is not implemented.");
        if (_environment.IsProduction() && !descriptor.IsProductionReady)
            throw new InvalidOperationException("This payment provider is not approved for production use.");
        if (!descriptor.IsProductionReady && draft.Environment.Equals("PRODUCTION", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This payment provider is not approved for production use.");
    }

    private Dictionary<string, string> Unprotect(string protectedConfiguration)
    {
        if (string.IsNullOrWhiteSpace(protectedConfiguration))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var json = _protector.Unprotect(protectedConfiguration);
        var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        return new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> NormalizeSettings(IReadOnlyDictionary<string, string> settings)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in settings)
        {
            var key = item.Key.Trim();
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Payment provider setting keys cannot be blank.");
            result[key] = item.Value?.Trim() ?? string.Empty;
        }
        return result;
    }

    private static string NormalizeKey(string value) => NormalizeKey(value, "Key", 80);

    private static string NormalizeKey(string value, string label, int maximumLength) =>
        RequireText(value, label, maximumLength).Replace('-', '_').Replace(' ', '_').ToUpperInvariant();

    private static string RequireText(string value, string label, int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException($"{label} is required.");
        if (normalized.Length > maximumLength)
            throw new InvalidOperationException($"{label} cannot exceed {maximumLength} characters.");
        return normalized;
    }
}

public sealed class PaymentRoutingService : IPaymentRoutingService
{
    private readonly AppDbContext _db;
    private readonly IPaymentProviderRegistry _registry;
    private readonly IHostEnvironment _environment;

    public PaymentRoutingService(
        AppDbContext db,
        IPaymentProviderRegistry registry,
        IHostEnvironment environment)
    {
        _db = db;
        _registry = registry;
        _environment = environment;
    }

    public async Task<PaymentRouteSelection> SelectInitialRouteAsync(
        Guid paymentMethodDefinitionId,
        CancellationToken cancellationToken = default)
    {
        var method = await _db.PaymentMethodDefinitions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == paymentMethodDefinitionId && x.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Active payment method not found.");

        var routes = await _db.PaymentProviderRoutes.AsNoTracking()
            .Include(x => x.PaymentProviderConfiguration)
            .Where(x => x.PaymentMethodDefinitionId == paymentMethodDefinitionId &&
                        x.IsEnabled &&
                        x.PaymentProviderConfiguration != null &&
                        x.PaymentProviderConfiguration.IsEnabled)
            .OrderBy(x => x.Priority)
            .ToListAsync(cancellationToken);

        foreach (var route in routes)
        {
            var configuration = route.PaymentProviderConfiguration!;
            var descriptor = _registry.GetRequired(configuration.ProviderKey).Descriptor;
            if (!descriptor.IsImplemented)
                continue;
            if (_environment.IsProduction() && !descriptor.IsProductionReady)
                continue;
            if (!descriptor.SupportedMethodCodes.Contains(method.Code, StringComparer.OrdinalIgnoreCase))
                continue;

            return new PaymentRouteSelection(
                configuration.Id,
                configuration.ProviderKey,
                route.ProviderPaymentMethodCode,
                route.Priority);
        }

        throw new InvalidOperationException(
            $"No active payment provider route is available for '{method.Name}'. Configure an implemented provider before accepting payments.");
    }

    public async Task<IReadOnlyList<PaymentMethodAvailability>> GetAvailableMethodsAsync(
        CancellationToken cancellationToken = default)
    {
        var candidates = await _db.PaymentProviderRoutes.AsNoTracking()
            .Include(x => x.PaymentMethodDefinition)
            .Include(x => x.PaymentProviderConfiguration)
            .Where(x => x.IsEnabled &&
                        x.PaymentMethodDefinition != null && x.PaymentMethodDefinition.IsActive &&
                        x.PaymentProviderConfiguration != null && x.PaymentProviderConfiguration.IsEnabled)
            .OrderBy(x => x.PaymentMethodDefinition!.DisplayOrder)
            .ThenBy(x => x.Priority)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(route => IsOperational(route.PaymentProviderConfiguration!, route.PaymentMethodDefinition!.Code))
            .GroupBy(route => route.PaymentMethodDefinitionId)
            .Select(group => group.First())
            .Select(route => new PaymentMethodAvailability(
                route.PaymentMethodDefinition!.Id,
                route.PaymentMethodDefinition.Code,
                route.PaymentMethodDefinition.Name,
                route.PaymentMethodDefinition.Category,
                route.PaymentProviderConfiguration!.DisplayName,
                route.PaymentProviderConfiguration.Environment,
                route.PaymentProviderConfiguration.Environment.Equals("SANDBOX", StringComparison.OrdinalIgnoreCase) &&
                    route.PaymentProviderConfiguration.ProviderKey == MtnMomoProfile.ProviderKey ? "EUR" : null,
                route.PaymentProviderConfiguration.Environment.Equals("SANDBOX", StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private bool IsOperational(PaymentProviderConfiguration configuration, string paymentMethodCode)
    {
        var descriptor = _registry.GetRequired(configuration.ProviderKey).Descriptor;
        return descriptor.IsImplemented &&
               (!_environment.IsProduction() || descriptor.IsProductionReady) &&
               descriptor.SupportedMethodCodes.Contains(paymentMethodCode, StringComparer.OrdinalIgnoreCase);
    }
}
