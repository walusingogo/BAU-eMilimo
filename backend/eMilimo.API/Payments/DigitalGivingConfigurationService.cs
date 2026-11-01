using System.Text.RegularExpressions;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed partial class DigitalGivingConfigurationService : IDigitalGivingConfigurationService
{
    private readonly AppDbContext _db;
    private readonly IPaymentRoutingService _routing;
    private readonly IContributionDestinationValidationService _destinations;

    public DigitalGivingConfigurationService(
        AppDbContext db,
        IPaymentRoutingService routing,
        IContributionDestinationValidationService destinations)
    {
        _db = db;
        _routing = routing;
        _destinations = destinations;
    }

    public async Task<DigitalGivingAdministrationView?> GetAdministrationViewAsync(
        CancellationToken cancellationToken = default)
    {
        var configuration = await ConfigurationQuery().SingleOrDefaultAsync(cancellationToken);
        return configuration is null ? null : await ToAdministrationViewAsync(configuration, cancellationToken);
    }

    public async Task<DigitalGivingConfigurationView?> GetPublicViewAsync(
        string publicSlug,
        CancellationToken cancellationToken = default)
    {
        var slug = NormalizeSlug(publicSlug);
        var configuration = await ConfigurationQuery()
            .SingleOrDefaultAsync(x => x.IsEnabled && x.PublicSlug == slug, cancellationToken);
        return configuration is null ? null : await ToPublicViewAsync(configuration, cancellationToken);
    }

    public async Task<DigitalGivingConfigurationView?> GetDefaultPublicViewAsync(
        CancellationToken cancellationToken = default)
    {
        var configuration = await ConfigurationQuery()
            .SingleOrDefaultAsync(x => x.IsEnabled, cancellationToken);
        return configuration is null ? null : await ToPublicViewAsync(configuration, cancellationToken);
    }

    public async Task<DigitalGivingAdministrationView> SaveAsync(
        DigitalGivingConfigurationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var digitalGivingSource = await _db.ContributionSources.SingleOrDefaultAsync(
            x => x.IsActive && x.Name.ToUpper() == "DIGITAL GIVING", cancellationToken)
            ?? throw new InvalidOperationException("The Digital Giving contribution source is not configured.");

        var requestedTypeIds = request.ContributionTypes.Select(x => x.ContributionTypeId).Distinct().ToArray();
        if (requestedTypeIds.Length != request.ContributionTypes.Count)
            throw new InvalidOperationException("Each contribution type may only be configured once.");
        var activeTypes = await _db.ContributionTypes
            .Where(x => requestedTypeIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);
        if (activeTypes.Count != requestedTypeIds.Length)
            throw new InvalidOperationException("Every Digital Giving contribution type must be active.");

        var publicTypeIds = request.ContributionTypes
            .Where(x => x.IsPubliclyAvailable)
            .Select(x => x.ContributionTypeId)
            .ToHashSet();
        foreach (var type in activeTypes)
            type.IsEnabledForDigitalGiving = publicTypeIds.Contains(type.Id);

        await ValidateDestinationsAsync(request.ContributionTypes, cancellationToken);
        await ValidateRoutesAsync(request.ProviderRoutes, cancellationToken);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var configuration = await _db.DigitalGivingConfigurations
            .SingleOrDefaultAsync(cancellationToken);
        var now = DateTime.UtcNow;
        if (configuration is null)
        {
            configuration = new DigitalGivingConfiguration
            {
                Id = Guid.NewGuid(),
                CreatedById = actorUserId,
                CreatedDate = now,
                Version = 1
            };
            _db.DigitalGivingConfigurations.Add(configuration);
        }
        else
        {
            await _db.DigitalGivingTypeSettings
                .Where(x => x.DigitalGivingConfigurationId == configuration.Id)
                .ExecuteDeleteAsync(cancellationToken);
            configuration.UpdatedById = actorUserId;
            configuration.Version++;
        }

        configuration.IsEnabled = request.IsEnabled;
        configuration.PublicSlug = NormalizeSlug(request.PublicSlug);
        configuration.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        configuration.MinimumAmount = decimal.Round(request.MinimumAmount, 2, MidpointRounding.AwayFromZero);
        configuration.MaximumAmount = request.MaximumAmount.HasValue
            ? decimal.Round(request.MaximumAmount.Value, 2, MidpointRounding.AwayFromZero)
            : null;
        configuration.ContributionSourceId = digitalGivingSource.Id;
        configuration.WelcomeMessage = string.IsNullOrWhiteSpace(request.WelcomeMessage)
            ? null
            : request.WelcomeMessage.Trim();
        configuration.UpdatedDate = now;

        foreach (var type in request.ContributionTypes)
        {
            _db.DigitalGivingTypeSettings.Add(new DigitalGivingTypeSetting
            {
                Id = Guid.NewGuid(),
                DigitalGivingConfigurationId = configuration.Id,
                ContributionTypeId = type.ContributionTypeId,
                IsPubliclyAvailable = type.IsPubliclyAvailable,
                DisplayOrder = type.DisplayOrder,
                DestinationType = type.DestinationType,
                GroupId = type.GroupId,
                SectionId = type.SectionId,
                CommitteeId = type.CommitteeId,
                CreatedById = actorUserId,
                CreatedDate = now,
                UpdatedDate = now,
                Version = 1
            });
        }

        await _db.PaymentProviderRoutes.ExecuteDeleteAsync(cancellationToken);
        foreach (var route in request.ProviderRoutes)
        {
            _db.PaymentProviderRoutes.Add(new PaymentProviderRoute
            {
                Id = Guid.NewGuid(),
                PaymentMethodDefinitionId = route.PaymentMethodDefinitionId,
                PaymentProviderConfigurationId = route.PaymentProviderConfigurationId,
                ProviderPaymentMethodCode = route.ProviderPaymentMethodCode.Trim(),
                Priority = route.Priority,
                IsEnabled = route.IsEnabled,
                CreatedById = actorUserId,
                CreatedDate = now,
                UpdatedDate = now,
                Version = 1
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (configuration.IsEnabled && !(await _routing.GetAvailableMethodsAsync(cancellationToken)).Any())
            throw new InvalidOperationException("Digital Giving cannot be published until at least one operational payment provider route is enabled.");
        await transaction.CommitAsync(cancellationToken);
        _db.ChangeTracker.Clear();
        return await GetAdministrationViewAsync(cancellationToken)
            ?? throw new InvalidOperationException("Digital Giving configuration could not be reloaded.");
    }

    public async Task<DigitalGivingMaintenanceCatalog> GetMaintenanceCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var contributionTypes = await _db.ContributionTypes.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new DigitalGivingMaintenanceOption(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);
        var contributionSources = await _db.ContributionSources.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new DigitalGivingMaintenanceOption(x.Id, x.Name, x.Name))
            .ToListAsync(cancellationToken);
        var groups = await _db.Groups.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.GroupName)
            .Select(x => new DigitalGivingMaintenanceOption(x.Id, x.GroupType, x.GroupName))
            .ToListAsync(cancellationToken);
        var sections = await _db.Sections.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SectionNumber).ThenBy(x => x.SectionName)
            .Select(x => new DigitalGivingMaintenanceOption(x.Id, $"SECTION_{x.SectionNumber}", x.SectionName))
            .ToListAsync(cancellationToken);
        var committees = await _db.Committees.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.CommitteeName)
            .Select(x => new DigitalGivingMaintenanceOption(x.Id, "COMMITTEE", x.CommitteeName))
            .ToListAsync(cancellationToken);
        var paymentMethods = await _db.PaymentMethodDefinitions.AsNoTracking()
            .Where(x => x.IsActive && (x.Code == "AIRTEL_MONEY" || x.Code == "MTN_MOMO" || x.Code == "ZAMTEL_MONEY" || x.Code == "MOBILE_MONEY"))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new PaymentMethodAvailability(x.Id, x.Code, x.Name, x.Category, null, null, null, false))
            .ToListAsync(cancellationToken);

        return new DigitalGivingMaintenanceCatalog(
            contributionTypes,
            contributionSources,
            groups,
            sections,
            committees,
            paymentMethods);
    }

    private IQueryable<DigitalGivingConfiguration> ConfigurationQuery() =>
        _db.DigitalGivingConfigurations.AsNoTracking()
            .Include(x => x.ContributionTypes)
                .ThenInclude(x => x.ContributionType);

    private async Task<DigitalGivingAdministrationView> ToAdministrationViewAsync(
        DigitalGivingConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var routes = await _db.PaymentProviderRoutes.AsNoTracking()
            .OrderBy(x => x.PaymentMethodDefinitionId).ThenBy(x => x.Priority)
            .Select(x => new PaymentProviderRouteView(
                x.PaymentMethodDefinitionId,
                x.PaymentProviderConfigurationId,
                x.ProviderPaymentMethodCode,
                x.Priority,
                x.IsEnabled))
            .ToListAsync(cancellationToken);
        var types = configuration.ContributionTypes
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ContributionType!.Name)
            .Select(x => new DigitalGivingTypeConfigurationView(
                x.ContributionTypeId,
                x.ContributionType?.Code ?? string.Empty,
                x.ContributionType?.Name ?? "Contribution",
                x.IsPubliclyAvailable,
                x.DisplayOrder,
                x.GroupId,
                x.SectionId,
                x.CommitteeId,
                x.SectionId.HasValue ? DigitalGivingDestinationTypes.Section :
                    x.GroupId.HasValue ? DigitalGivingDestinationTypes.Group :
                    x.CommitteeId.HasValue ? DigitalGivingDestinationTypes.Committee :
                    x.DestinationType))
            .ToArray();
        return new DigitalGivingAdministrationView(
            configuration.Id,
            configuration.IsEnabled,
            configuration.PublicSlug,
            configuration.CurrencyCode,
            configuration.MinimumAmount,
            configuration.MaximumAmount,
            configuration.ContributionSourceId,
            configuration.WelcomeMessage,
            types,
            routes);
    }

    private async Task<DigitalGivingConfigurationView> ToPublicViewAsync(
        DigitalGivingConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var methods = await _routing.GetAvailableMethodsAsync(cancellationToken);
        var types = new List<DigitalGivingTypeView>();
        foreach (var setting in configuration.ContributionTypes
                     .Where(x => x.IsPubliclyAvailable && x.ContributionType is { IsActive: true, IsEnabledForDigitalGiving: true })
                     .OrderBy(x => x.DisplayOrder)
                     .ThenBy(x => x.ContributionType!.Name))
        {
            var contributionType = setting.ContributionType!;
            var eligible = await _destinations.GetEligibleDestinationsAsync(
                setting.ContributionTypeId,
                cancellationToken: cancellationToken);
            var configuredDestinationType = setting.SectionId.HasValue ? DigitalGivingDestinationTypes.Section :
                setting.GroupId.HasValue ? DigitalGivingDestinationTypes.Group :
                setting.CommitteeId.HasValue ? DigitalGivingDestinationTypes.Committee :
                string.IsNullOrWhiteSpace(setting.DestinationType)
                ? contributionType.DefaultDigitalDestinationType ?? DigitalGivingDestinationTypes.Organization
                : setting.DestinationType.Trim().ToUpperInvariant();
            var fixedDestinationId = setting.SectionId ?? setting.GroupId ?? setting.CommitteeId;
            var filteredEligible = eligible.Where(x =>
                string.Equals(x.DestinationType, configuredDestinationType, StringComparison.OrdinalIgnoreCase) &&
                (!fixedDestinationId.HasValue || x.Id == fixedDestinationId.Value));
            var options = filteredEligible.Select(x => new DigitalGivingDestinationOption(
                x.DestinationType == "ORGANIZATION" ? null : x.Id,
                x.Name,
                x.DestinationType)).ToArray();
            if (options.Length == 0)
                throw new InvalidOperationException(
                    $"Contribution type '{contributionType.Name}' has no active {configuredDestinationType.ToLowerInvariant()} destination available for Digital Giving.");
            var requiresSelection = configuredDestinationType != DigitalGivingDestinationTypes.Organization &&
                                    !fixedDestinationId.HasValue &&
                                    (contributionType.RequireDestinationSelectionForDigitalGiving || options.Length > 1);
            types.Add(new DigitalGivingTypeView(
                setting.ContributionTypeId,
                contributionType.Code,
                contributionType.Name,
                setting.DisplayOrder,
                configuredDestinationType,
                requiresSelection,
                options));
        }
        var churchName = await _db.SystemConfigs.AsNoTracking()
            .Where(x => !x.IsArchived && x.ConfigKey == "church_name")
            .Select(x => x.ConfigValue)
            .FirstOrDefaultAsync(cancellationToken) ?? "Church Administration";
        var sandboxScenarios = methods.Any(x => x.IsSandbox)
            ? await _db.MtnSandboxScenarios.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new PublicSandboxScenarioView(
                    x.Id, x.Name, x.Description, x.Msisdn,
                    x.ExpectedTransactionStatus, x.ExpectedProcessingBehaviour))
                .ToListAsync(cancellationToken)
            : [];
        return new DigitalGivingConfigurationView(
            configuration.Id,
            churchName,
            configuration.IsEnabled,
            configuration.PublicSlug,
            configuration.CurrencyCode,
            configuration.MinimumAmount,
            configuration.MaximumAmount,
            configuration.ContributionSourceId,
            configuration.WelcomeMessage,
            types,
            methods,
            sandboxScenarios);
    }

    private async Task ValidateDestinationsAsync(
        IReadOnlyList<DigitalGivingTypeConfigurationRequest> types,
        CancellationToken cancellationToken)
    {
        foreach (var type in types.Where(x => x.IsPubliclyAvailable))
        {
            var contributionType = await _db.ContributionTypes.AsNoTracking()
                .SingleAsync(x => x.Id == type.ContributionTypeId && x.IsActive, cancellationToken);
            var destinationType = type.DestinationType.Trim().ToUpperInvariant();
            var destinationCount = (type.GroupId.HasValue ? 1 : 0) +
                                   (type.SectionId.HasValue ? 1 : 0) +
                                   (type.CommitteeId.HasValue ? 1 : 0);
            if (destinationCount > 1)
                throw new InvalidOperationException("A public contribution type may configure at most one legacy fixed destination.");
            if (!DigitalGivingDestinationTypes.All.Contains(type.DestinationType, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Select a valid Digital Giving destination type.");
            var allowed = destinationType switch
            {
                DigitalGivingDestinationTypes.Organization => contributionType.AllowOrganizationWideAllocation,
                DigitalGivingDestinationTypes.Section => contributionType.AllowSectionAllocation,
                DigitalGivingDestinationTypes.Group => contributionType.AllowGroupAllocation,
                DigitalGivingDestinationTypes.Committee => contributionType.AllowCommitteeAllocation,
                _ => false
            };
            if (!allowed)
                throw new InvalidOperationException(
                    $"Contribution type '{contributionType.Name}' does not allow {destinationType.ToLowerInvariant()} allocation.");
            if (type.GroupId.HasValue && destinationType != DigitalGivingDestinationTypes.Group ||
                type.SectionId.HasValue && destinationType != DigitalGivingDestinationTypes.Section ||
                type.CommitteeId.HasValue && destinationType != DigitalGivingDestinationTypes.Committee)
                throw new InvalidOperationException("The fixed destination must match the selected destination type.");
            if (type.GroupId.HasValue && !await _db.Groups.AnyAsync(x => x.Id == type.GroupId && x.IsActive, cancellationToken))
                throw new InvalidOperationException("A configured Digital Giving group is unavailable.");
            if (type.SectionId.HasValue && !await _db.Sections.AnyAsync(x => x.Id == type.SectionId && x.IsActive, cancellationToken))
                throw new InvalidOperationException("A configured Digital Giving section is unavailable.");
            if (type.CommitteeId.HasValue && !await _db.Committees.AnyAsync(x => x.Id == type.CommitteeId && x.IsActive, cancellationToken))
                throw new InvalidOperationException("A configured Digital Giving committee is unavailable.");
        }
    }

    private async Task ValidateRoutesAsync(
        IReadOnlyList<PaymentProviderRouteRequest> routes,
        CancellationToken cancellationToken)
    {
        if (routes.GroupBy(x => new { x.PaymentMethodDefinitionId, x.PaymentProviderConfigurationId }).Any(x => x.Count() > 1))
            throw new InvalidOperationException("A provider may only be configured once for each payment method.");
        if (routes.GroupBy(x => new { x.PaymentMethodDefinitionId, x.Priority }).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Provider priorities must be unique within each payment method.");
        if (routes.Any(x => x.Priority < 0 || string.IsNullOrWhiteSpace(x.ProviderPaymentMethodCode)))
            throw new InvalidOperationException("Every provider route requires a non-negative priority and provider method code.");

        var methodIds = routes.Select(x => x.PaymentMethodDefinitionId).Distinct().ToArray();
        var providerIds = routes.Select(x => x.PaymentProviderConfigurationId).Distinct().ToArray();
        if (await _db.PaymentMethodDefinitions.CountAsync(x => methodIds.Contains(x.Id) && x.IsActive, cancellationToken) != methodIds.Length)
            throw new InvalidOperationException("Every provider route requires an active payment method.");
        if (await _db.PaymentProviderConfigurations.CountAsync(x => providerIds.Contains(x.Id), cancellationToken) != providerIds.Length)
            throw new InvalidOperationException("A configured payment provider could not be found.");
    }

    private static void ValidateRequest(DigitalGivingConfigurationRequest request)
    {
        _ = NormalizeSlug(request.PublicSlug);
        if (request.CurrencyCode.Trim().Length != 3)
            throw new InvalidOperationException("Digital Giving currency must be a three-letter code.");
        if (request.MinimumAmount <= 0 || request.MaximumAmount.HasValue && request.MaximumAmount < request.MinimumAmount)
            throw new InvalidOperationException("Digital Giving amount limits are invalid.");
        if (request.IsEnabled && !request.ContributionTypes.Any(x => x.IsPubliclyAvailable))
            throw new InvalidOperationException("Enable at least one contribution type before enabling Digital Giving.");
        if (request.WelcomeMessage?.Length > 250)
            throw new InvalidOperationException("The welcome message cannot exceed 250 characters.");
    }

    private static string NormalizeSlug(string value)
    {
        var slug = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (slug.Length is < 3 or > 80 || !SlugPattern().IsMatch(slug))
            throw new InvalidOperationException("The public slug must contain 3-80 lowercase letters, numbers or single hyphens.");
        return slug;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
