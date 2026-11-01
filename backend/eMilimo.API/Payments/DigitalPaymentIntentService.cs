using System.Data;
using System.Globalization;
using System.Text.Json;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed class DigitalPaymentIntentService : IDigitalPaymentIntentService
{
    private readonly AppDbContext _db;
    private readonly IPaymentRoutingService _routing;
    private readonly IPaymentProviderRegistry _providers;
    private readonly IPaymentProviderConfigurationStore _providerConfigurations;
    private readonly IReferenceNumberService _referenceNumbers;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IDataProtector _payerProtector;
    private readonly IDataProtector _statusTokenProtector;
    private readonly ILogger<DigitalPaymentIntentService> _logger;
    private readonly IContributionDestinationValidationService _destinationValidation;

    public DigitalPaymentIntentService(
        AppDbContext db,
        IPaymentRoutingService routing,
        IPaymentProviderRegistry providers,
        IPaymentProviderConfigurationStore providerConfigurations,
        IReferenceNumberService referenceNumbers,
        IHostEnvironment environment,
        IConfiguration configuration,
        IDataProtectionProvider dataProtection,
        IContributionDestinationValidationService destinationValidation,
        ILogger<DigitalPaymentIntentService> logger)
    {
        _db = db;
        _routing = routing;
        _providers = providers;
        _providerConfigurations = providerConfigurations;
        _referenceNumbers = referenceNumbers;
        _environment = environment;
        _configuration = configuration;
        _payerProtector = dataProtection.CreateProtector("eMilimo.DigitalGiving.PayerData.v1");
        _statusTokenProtector = dataProtection.CreateProtector("eMilimo.DigitalGiving.PublicStatusToken.v1");
        _destinationValidation = destinationValidation;
        _logger = logger;
    }

    public async Task<DigitalPaymentIntentResult> CreateAsync(
        CreateDigitalPaymentIntentRequest request,
        string clientFingerprint,
        CancellationToken cancellationToken = default)
    {
        ValidateBasicRequest(request);
        var normalizedPhone = DigitalGivingSecurity.NormalizePhone(request.PaymentPhone);
        var fingerprint = Fingerprint(request, normalizedPhone);
        var idempotencyKey = request.IdempotencyKey.Trim();
        var existing = await FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
            return Replay(existing, fingerprint);

        var configuration = await _db.DigitalGivingConfigurations
            .Include(x => x.ContributionTypes).ThenInclude(x => x.ContributionType)
            .SingleOrDefaultAsync(x => x.IsEnabled && x.PublicSlug == request.PublicSlug.Trim().ToLower(), cancellationToken)
            ?? throw new KeyNotFoundException("Digital Giving page not found or is not accepting payments.");
        var route = await _routing.SelectInitialRouteAsync(request.PaymentMethodDefinitionId, cancellationToken);
        var providerSettings = await _providerConfigurations.GetRuntimeSettingsAsync(route.ProviderConfigurationId, true, cancellationToken);
        var isSandbox = string.Equals(providerSettings.GetValueOrDefault("_Environment"), "SANDBOX", StringComparison.OrdinalIgnoreCase);
        var effectiveCurrency = isSandbox && route.ProviderKey == MtnMomoProfile.ProviderKey
            ? "EUR"
            : configuration.CurrencyCode;
        ValidateAmountAndCurrency(request, configuration, effectiveCurrency);
        var configuredTypes = configuration.ContributionTypes
            .Where(x => x.IsPubliclyAvailable && x.ContributionType is { IsActive: true, IsEnabledForDigitalGiving: true })
            .ToDictionary(x => x.ContributionTypeId);
        ValidateLines(request, configuredTypes);
        var resolvedLines = await ResolveAndValidateLinesAsync(request.Lines, configuredTypes, cancellationToken);

        var method = await _db.PaymentMethodDefinitions.AsNoTracking()
            .SingleAsync(x => x.Id == request.PaymentMethodDefinitionId, cancellationToken);
        var now = DateTime.UtcNow;
        var publicToken = DigitalGivingSecurity.NewOpaqueToken();
        var paymentReference = await _referenceNumbers.NextAsync("DIGITAL_GIVING", cancellationToken: cancellationToken);
        var transactionEntity = new DigitalCollection
        {
            Id = Guid.NewGuid(),
            PaymentReference = paymentReference,
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = fingerprint,
            PublicStatusTokenHash = DigitalGivingSecurity.Hash(publicToken),
            ProtectedPublicStatusToken = _statusTokenProtector.Protect(publicToken),
            GiverType = request.GiverType.Trim().ToUpperInvariant(),
            PaymentMethodDefinitionId = method.Id,
            ContributionSourceId = configuration.ContributionSourceId,
            PaymentProviderConfigurationId = route.ProviderConfigurationId,
            Status = PaymentTransactionStatuses.Created,
            SettlementStatus = PaymentSettlementStatuses.Pending,
            CurrencyCode = effectiveCurrency,
            GrossAmount = decimal.Round(request.TotalAmount, 2, MidpointRounding.AwayFromZero),
            ProviderFee = 0m,
            NetSettlementAmount = 0m,
            ProviderConfigurationVersion = int.TryParse(providerSettings.GetValueOrDefault("_ConfigurationVersion"), out var configurationVersion) ? configurationVersion : 1,
            ProviderEnvironmentSnapshot = providerSettings.GetValueOrDefault("_Environment") ?? "SANDBOX",
            ReconciliationStatus = PaymentReconciliationStatuses.None,
            CreatedDate = now,
            UpdatedDate = now,
            Version = 1
        };

        await using (var databaseTransaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
        {
            await ApplyContributorIdentityAsync(transactionEntity, request, clientFingerprint, now, cancellationToken);
            transactionEntity.ProtectedPayerData = _payerProtector.Protect(JsonSerializer.Serialize(new ProtectedPayerData(
                normalizedPhone,
                string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(),
                transactionEntity.GiverType == DigitalGiverTypes.Guest ? transactionEntity.ContributorNameSnapshot : null)));
            foreach (var resolved in resolvedLines)
            {
                transactionEntity.Lines.Add(new DigitalCollectionItem
                {
                    Id = Guid.NewGuid(),
                    ContributionTypeId = resolved.Line.ContributionTypeId,
                    Amount = decimal.Round(resolved.Line.Amount, 2, MidpointRounding.AwayFromZero),
                    GroupId = resolved.DestinationType == "GROUP" ? resolved.DestinationId : null,
                    SectionId = resolved.DestinationType == "SECTION" ? resolved.DestinationId : null,
                    CommitteeId = resolved.DestinationType == "COMMITTEE" ? resolved.DestinationId : null,
                    CreatedDate = now,
                    UpdatedDate = now,
                    Version = 1
                });
            }
            transactionEntity.Events.Add(NewEvent(transactionEntity.Id, "INTENT_CREATED", null, PaymentTransactionStatuses.Created, "PUBLIC_API", now));
            _db.DigitalCollections.Add(transactionEntity);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                await databaseTransaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await databaseTransaction.RollbackAsync(cancellationToken);
                _db.ChangeTracker.Clear();
                existing = await FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
                if (existing is not null)
                    return Replay(existing, fingerprint);
                throw;
            }
        }

        var provider = _providers.GetRequired(route.ProviderKey);
        PaymentInitiationResult initiation;
        try
        {
            initiation = await provider.InitiateAsync(new PaymentInitiationRequest(
                transactionEntity.Id,
                paymentReference,
                route.ProviderPaymentMethodCode,
                transactionEntity.GrossAmount,
                transactionEntity.CurrencyCode,
                normalizedPhone,
                BuildWebhookUrl(route.ProviderKey),
                idempotencyKey,
                providerSettings), cancellationToken);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment initiation failed for {PaymentReference} through {ProviderKey}.", paymentReference, route.ProviderKey);
            var ambiguous = ex is PaymentProviderException { IsAmbiguous: true };
            transactionEntity.Status = ambiguous ? PaymentTransactionStatuses.Unknown : PaymentTransactionStatuses.Failed;
            transactionEntity.ReconciliationStatus = ambiguous ? PaymentReconciliationStatuses.Scheduled : PaymentReconciliationStatuses.None;
            transactionEntity.ReconciliationAttemptCount = 0;
            transactionEntity.NextReconciliationAtUtc = ambiguous
                ? DateTime.UtcNow.AddSeconds(ParseSetting(providerSettings, "PollingInitialSeconds", 20))
                : null;
            transactionEntity.FailureReason = Truncate(ex.Message, 1000);
            transactionEntity.UpdatedDate = DateTime.UtcNow;
            transactionEntity.Version++;
            _db.DigitalCollectionEvents.Add(NewEvent(
                transactionEntity.Id,
                ambiguous ? "PROVIDER_INITIATION_UNCERTAIN" : "PROVIDER_INITIATION_FAILED",
                PaymentTransactionStatuses.Created,
                transactionEntity.Status,
                route.ProviderKey,
                transactionEntity.UpdatedDate));
            await _db.SaveChangesAsync(cancellationToken);
            if (ambiguous)
                throw new PaymentConflictException(
                    $"The payment request outcome is uncertain. Do not submit another payment. eMilimo will reconcile reference {paymentReference} safely.");
            throw new InvalidOperationException("The payment provider rejected this payment. No alternative provider was attempted.", ex);
        }

        var normalizedInitiationStatus = initiation.Status.ToUpperInvariant();
        transactionEntity.ProviderTransactionId = initiation.ProviderTransactionId;
        transactionEntity.InitiatedAtUtc = DateTime.UtcNow;
        transactionEntity.ExpiresAtUtc = initiation.ExpiresAtUtc;
        transactionEntity.Status = normalizedInitiationStatus is PaymentTransactionStatuses.Failed or PaymentTransactionStatuses.Cancelled
            ? normalizedInitiationStatus
            : PaymentTransactionStatuses.Pending;
        transactionEntity.ReconciliationStatus = transactionEntity.Status == PaymentTransactionStatuses.Pending
            ? PaymentReconciliationStatuses.Scheduled
            : PaymentReconciliationStatuses.None;
        transactionEntity.NextReconciliationAtUtc = transactionEntity.Status == PaymentTransactionStatuses.Pending
            ? DateTime.UtcNow.AddSeconds(ParseSetting(providerSettings, "PollingInitialSeconds", 20))
            : null;
        transactionEntity.FailureReason = transactionEntity.Status == PaymentTransactionStatuses.Failed
            ? Truncate(initiation.ProviderMessage, 1000)
            : null;
        transactionEntity.UpdatedDate = DateTime.UtcNow;
        transactionEntity.Version++;
        _db.DigitalCollectionEvents.Add(NewEvent(
            transactionEntity.Id,
            "PROVIDER_INITIATED",
            PaymentTransactionStatuses.Created,
            transactionEntity.Status,
            route.ProviderKey,
            transactionEntity.UpdatedDate));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // The provider may already have accepted the charge. Never relabel it as
            // failed or route it elsewhere; the provider reference is required for
            // reconciliation and the caller may safely replay the idempotency key.
            _logger.LogCritical(
                ex,
                "Provider {ProviderKey} accepted payment {PaymentReference} as {ProviderTransactionId}, but the initiation result could not be persisted.",
                route.ProviderKey,
                paymentReference,
                initiation.ProviderTransactionId);
            throw new PaymentConflictException(
                "The provider accepted the payment request, but its status is awaiting reconciliation. Do not submit another payment; retry with the same idempotency key or contact support with the payment reference.");
        }

        return Result(transactionEntity, publicToken, initiation.ProviderMessage, false);
    }

    private static int ParseSetting(IReadOnlyDictionary<string, string> settings, string key, int fallback) =>
        settings.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;

    public async Task<DigitalPaymentStatusView> GetStatusAsync(
        string paymentReference,
        string publicStatusToken,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.DigitalCollections.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(x => x.ContributionType)
            .Include(x => x.Lines).ThenInclude(x => x.Contribution)
            .SingleOrDefaultAsync(x => x.PaymentReference == paymentReference.Trim().ToUpperInvariant(), cancellationToken)
            ?? throw new KeyNotFoundException("Payment not found.");
        if (!DigitalGivingSecurity.FixedTimeEquals(entity.PublicStatusTokenHash, publicStatusToken))
            throw new KeyNotFoundException("Payment not found.");
        var churchName = await _db.SystemConfigs.AsNoTracking()
            .Where(x => !x.IsArchived && x.ConfigKey == "church_name")
            .Select(x => x.ConfigValue)
            .FirstOrDefaultAsync(cancellationToken) ?? "Church Administration";
        return new DigitalPaymentStatusView(
            churchName,
            entity.PaymentReference,
            entity.Status,
            entity.SettlementStatus,
            entity.GrossAmount,
            entity.ProviderFee,
            entity.NetSettlementAmount,
            entity.CurrencyCode,
            entity.CreatedDate,
            entity.ProviderCompletedAtUtc,
            entity.Lines.Select(x => new DigitalPaymentStatusLine(
                x.ContributionType?.Name ?? "Contribution",
                x.Amount,
                x.Contribution?.ContributionNumber)).ToArray(),
            entity.ProviderEnvironmentSnapshot.Equals("SANDBOX", StringComparison.OrdinalIgnoreCase));
    }

    private async Task ApplyContributorIdentityAsync(
        DigitalCollection entity,
        CreateDigitalPaymentIntentRequest request,
        string clientFingerprint,
        DateTime now,
        CancellationToken cancellationToken)
    {
        switch (entity.GiverType)
        {
            case DigitalGiverTypes.Member:
                if (!request.MemberVerificationChallengeId.HasValue || string.IsNullOrWhiteSpace(request.MemberVerificationToken))
                    throw new InvalidOperationException("Complete member verification before creating the payment.");
                var challenge = await _db.MemberVerificationChallenges
                    .Include(x => x.Member)
                    .SingleOrDefaultAsync(x => x.Id == request.MemberVerificationChallengeId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Member verification is invalid or has expired.");
                if (!challenge.IsVerified || challenge.Member is null || challenge.ConsumedAtUtc.HasValue ||
                    challenge.ExpiresAtUtc <= now ||
                    challenge.ClientFingerprintHash != DigitalGivingSecurity.Hash(clientFingerprint) ||
                    challenge.VerificationTokenHash is null ||
                    !DigitalGivingSecurity.FixedTimeEquals(challenge.VerificationTokenHash, request.MemberVerificationToken))
                    throw new InvalidOperationException("Member verification is invalid or has expired.");
                entity.MemberId = challenge.MemberId;
                entity.ContributorNameSnapshot = challenge.Member.PreferredName ??
                    $"{challenge.Member.FirstName} {challenge.Member.LastName}".Trim();
                challenge.ConsumedAtUtc = now;
                challenge.UpdatedDate = now;
                challenge.Version++;
                break;
            case DigitalGiverTypes.Guest:
                var guestName = request.ContributorName?.Trim() ?? string.Empty;
                if (guestName.Length is < 2 or > 250)
                    throw new InvalidOperationException("Guest contributor name must contain 2-250 characters.");
                entity.ContributorNameSnapshot = guestName;
                break;
            case DigitalGiverTypes.Anonymous:
                entity.ContributorNameSnapshot = null;
                break;
            default:
                throw new InvalidOperationException("Select Member, Guest or Anonymous as the contributor type.");
        }
    }

    private async Task<DigitalCollection?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        await _db.DigitalCollections.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    private DigitalPaymentIntentResult Replay(DigitalCollection existing, string fingerprint)
    {
        if (existing.RequestFingerprint != fingerprint)
            throw new PaymentConflictException("This idempotency key was already used for a different payment request.");
        string publicToken;
        try { publicToken = _statusTokenProtector.Unprotect(existing.ProtectedPublicStatusToken); }
        catch { throw new InvalidOperationException("The existing payment status token can no longer be recovered. Contact support with the payment reference."); }
        return Result(existing, publicToken, "The original payment request was returned without initiating another charge.", true);
    }

    private static DigitalPaymentIntentResult Result(
        DigitalCollection entity,
        string publicToken,
        string? providerMessage,
        bool replay) => new(
            entity.Id,
            entity.PaymentReference,
            entity.Status,
            entity.GrossAmount,
            entity.CurrencyCode,
            publicToken,
            providerMessage,
            entity.ExpiresAtUtc,
            replay,
            entity.ProviderEnvironmentSnapshot.Equals("SANDBOX", StringComparison.OrdinalIgnoreCase));

    private Uri BuildWebhookUrl(string providerKey)
    {
        var configured = _configuration["DigitalGiving:PublicApiBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(configured))
        {
            if (!_environment.IsDevelopment())
                throw new InvalidOperationException("DigitalGiving:PublicApiBaseUrl must be configured before accepting production payments.");
            configured = "http://localhost:5290";
        }
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var publicApiUri) || publicApiUri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("DigitalGiving:PublicApiBaseUrl must be an absolute HTTP or HTTPS URL.");
        if (_environment.IsProduction() && !_configuration.GetValue<bool>("DigitalGiving:AllowLoopbackPublicUrls", false) &&
            (publicApiUri.IsLoopback || publicApiUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("DigitalGiving:PublicApiBaseUrl must be a non-loopback HTTPS URL before accepting production payments.");
        return new Uri(publicApiUri, $"/api/public/digital-giving/webhooks/{Uri.EscapeDataString(providerKey)}");
    }

    private static void ValidateBasicRequest(CreateDigitalPaymentIntentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Trim().Length is < 16 or > 120)
            throw new InvalidOperationException("A 16-120 character idempotency key is required.");
        if (request.Lines.Count is < 1 or > 20)
            throw new InvalidOperationException("A payment must contain between 1 and 20 contribution lines.");
    }

    private static void ValidateAmountAndCurrency(
        CreateDigitalPaymentIntentRequest request,
        DigitalGivingConfiguration configuration,
        string acceptedCurrency)
    {
        if (!string.Equals(request.CurrencyCode.Trim(), acceptedCurrency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"The selected payment method accepts {acceptedCurrency} in this environment.");
        var total = decimal.Round(request.TotalAmount, 2, MidpointRounding.AwayFromZero);
        if (total < configuration.MinimumAmount || configuration.MaximumAmount.HasValue && total > configuration.MaximumAmount)
            throw new InvalidOperationException("The payment total is outside the configured Digital Giving limits.");
    }

    private static void ValidateLines(
        CreateDigitalPaymentIntentRequest request,
        IReadOnlyDictionary<Guid, DigitalGivingTypeSetting> configuredTypes)
    {
        if (request.Lines.GroupBy(x => x.ContributionTypeId).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Each contribution category may only appear once in a payment.");
        if (request.Lines.Any(x => x.Amount <= 0 || !configuredTypes.ContainsKey(x.ContributionTypeId)))
            throw new InvalidOperationException("Every contribution line must use an available category and a positive amount.");
        var lineTotal = request.Lines.Sum(x => decimal.Round(x.Amount, 2, MidpointRounding.AwayFromZero));
        var requestTotal = decimal.Round(request.TotalAmount, 2, MidpointRounding.AwayFromZero);
        if (lineTotal != requestTotal)
            throw new InvalidOperationException("The payment total must equal the sum of its contribution lines.");
    }

    private async Task<IReadOnlyList<ResolvedGivingLine>> ResolveAndValidateLinesAsync(
        IReadOnlyList<DigitalGivingLineRequest> lines,
        IReadOnlyDictionary<Guid, DigitalGivingTypeSetting> configuredTypes,
        CancellationToken cancellationToken)
    {
        var resolved = new List<ResolvedGivingLine>(lines.Count);
        foreach (var line in lines)
        {
            var setting = configuredTypes[line.ContributionTypeId];
            var fixedDestinationId = setting.SectionId ?? setting.GroupId ?? setting.CommitteeId;
            var fixedDestinationType = setting.SectionId.HasValue ? "SECTION" :
                setting.GroupId.HasValue ? "GROUP" :
                setting.CommitteeId.HasValue ? "COMMITTEE" : null;
            var configuredDestinationType = fixedDestinationType ?? (string.IsNullOrWhiteSpace(setting.DestinationType)
                ? setting.ContributionType?.DefaultDigitalDestinationType ?? "ORGANIZATION"
                : setting.DestinationType.Trim().ToUpperInvariant());
            var requestedDestinationType = string.IsNullOrWhiteSpace(line.DestinationType)
                ? configuredDestinationType
                : line.DestinationType.Trim().ToUpperInvariant();
            if (!string.Equals(requestedDestinationType, configuredDestinationType, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"The selected destination is not permitted for '{setting.ContributionType?.Name ?? "this contribution category"}'.");
            var destinationType = fixedDestinationType ?? configuredDestinationType;
            var destinationId = line.DestinationId == Guid.Empty
                ? null
                : line.DestinationId ?? fixedDestinationId;
            if (fixedDestinationId.HasValue && destinationId != fixedDestinationId)
                throw new InvalidOperationException(
                    $"The selected destination is not permitted for '{setting.ContributionType?.Name ?? "this contribution category"}'.");
            if (destinationType == "ORGANIZATION") destinationId = null;
            resolved.Add(new ResolvedGivingLine(line, destinationType, destinationId));
        }

        var errors = await _destinationValidation.ValidatePaymentLinesAsync(
            resolved.Select(x => new PaymentLineToValidate(
                x.Line.ContributionTypeId,
                x.Line.Amount,
                x.DestinationType,
                x.DestinationId)).ToArray(),
            cancellationToken);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));
        return resolved;
    }

    private static string Fingerprint(CreateDigitalPaymentIntentRequest request, string normalizedPhone)
    {
        var canonicalLines = string.Join(';', request.Lines
            .OrderBy(x => x.ContributionTypeId)
            .Select(x => $"{x.ContributionTypeId:N}:{decimal.Round(x.Amount, 2).ToString("F2", CultureInfo.InvariantCulture)}:{x.DestinationType?.Trim().ToUpperInvariant()}:{x.DestinationId?.ToString("N")}"));
        var canonical = string.Join('|',
            request.PublicSlug.Trim().ToLowerInvariant(),
            request.GiverType.Trim().ToUpperInvariant(),
            request.PaymentMethodDefinitionId.ToString("N"),
            decimal.Round(request.TotalAmount, 2).ToString("F2", CultureInfo.InvariantCulture),
            request.CurrencyCode.Trim().ToUpperInvariant(),
            normalizedPhone,
            request.ContributorName?.Trim() ?? string.Empty,
            request.Email?.Trim().ToLowerInvariant() ?? string.Empty,
            request.MemberVerificationChallengeId?.ToString("N") ?? string.Empty,
            canonicalLines);
        return DigitalGivingSecurity.Hash(canonical);
    }

    private static DigitalCollectionEvent NewEvent(
        Guid transactionId,
        string eventType,
        string? previousStatus,
        string newStatus,
        string source,
        DateTime occurredAt) => new()
        {
            Id = Guid.NewGuid(),
            DigitalCollectionId = transactionId,
            EventType = eventType,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Source = source,
            OccurredAtUtc = occurredAt,
            CreatedDate = occurredAt,
            UpdatedDate = occurredAt,
            Version = 1
        };

    private static string? Truncate(string? value, int maximumLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Length <= maximumLength ? value : value[..maximumLength];

    private sealed record ProtectedPayerData(string PhoneNumber, string? Email, string? GuestName);
    private sealed record ResolvedGivingLine(
        DigitalGivingLineRequest Line,
        string DestinationType,
        Guid? DestinationId);
}
