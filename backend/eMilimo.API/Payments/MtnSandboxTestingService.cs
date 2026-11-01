using System.Text.Json;
using System.Text.RegularExpressions;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed record MtnSandboxScenarioDraft(
    Guid? Id,
    string ScenarioCode,
    string Name,
    string Description,
    string Msisdn,
    string ExpectedTransactionStatus,
    int? ExpectedHttpResponseCode,
    string? ExpectedMtnErrorCode,
    decimal TestAmount,
    string Currency,
    string ExternalReferencePrefix,
    string PayerMessage,
    string PayeeNote,
    string ExpectedProcessingBehaviour,
    int MaximumPollingAttempts,
    int PollingIntervalSeconds,
    bool IsActive,
    string SourceClassification,
    string? SourceReference);

public sealed record MtnSandboxSampleDraft(
    Guid? Id,
    string Name,
    Guid? ContributionTypeId,
    string ContributionTypeName,
    decimal Amount,
    string Currency,
    string TestMsisdn,
    string ExternalReferenceFormat,
    string PayerMessage,
    string PayeeNote,
    Guid MtnSandboxScenarioId,
    bool IsActive);

public sealed record MtnSandboxSamplePreset(
    Guid Id, string Name, Guid? ContributionTypeId, string ContributionTypeName,
    decimal Amount, string Currency, string TestMsisdn, string ExternalReferenceFormat,
    string PayerMessage, string PayeeNote, Guid MtnSandboxScenarioId,
    bool IsActive, bool IsSystemDefault, int ConfigurationVersion,
    DateTime CreatedDate, DateTime UpdatedDate);

public sealed record MtnSandboxExecutionRequest(
    Guid ConfigurationId,
    Guid ScenarioId,
    Guid? SampleTransactionId,
    decimal? Amount,
    string? Currency,
    string? Msisdn,
    string? ExternalReferencePrefix,
    string? PayerMessage,
    string? PayeeNote);

public sealed class MtnSandboxTestingService(
    AppDbContext db,
    IPaymentProviderRegistry providers,
    IPaymentProviderConfigurationStore configurations,
    IHttpContextAccessor httpContext)
{
    private const string OfficialSource = "https://momodeveloper.mtn.com/content/html_widgets/6kfu8.html";
    private static readonly Regex CodePattern = new("^[A-Z0-9][A-Z0-9_-]{2,79}$", RegexOptions.Compiled);
    private static readonly string[] AllowedStatuses =
        [PaymentTransactionStatuses.Succeeded, PaymentTransactionStatuses.Failed, PaymentTransactionStatuses.Expired, PaymentTransactionStatuses.Pending, PaymentTransactionStatuses.Unknown];
    private static readonly string[] AllowedBehaviours =
        ["SUCCESS", "TERMINAL_FAILURE", "PENDING", "DELAYED", "REQUEST_ERROR"];
    private static readonly string[] AllowedSources = ["DOCUMENTED", "INFERRED", "CUSTOM"];

    public async Task<object> CatalogAsync(CancellationToken cancellationToken)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var scenarios = await db.MtnSandboxScenarios.AsNoTracking().OrderBy(x => x.Name).Select(x => new
        {
            x.Id, x.ScenarioCode, x.Name, x.Description, x.Msisdn, x.ExpectedTransactionStatus,
            x.ExpectedHttpResponseCode, x.ExpectedMtnErrorCode, x.TestAmount, x.Currency,
            x.ExternalReferencePrefix, x.PayerMessage, x.PayeeNote, x.ExpectedProcessingBehaviour,
            x.MaximumPollingAttempts, x.PollingIntervalSeconds, x.IsActive, x.SourceClassification,
            x.SourceReference, x.IsSystemDefault, x.ConfigurationVersion, x.CreatedDate, x.UpdatedDate
        }).ToListAsync(cancellationToken);
        var scenarioNames = scenarios.ToDictionary(x => x.Id, x => x.Name);
        var samples = (await db.MtnSandboxScenarios.AsNoTracking()
                .Select(x => new { x.Id, x.SamplePresetsJson }).ToListAsync(cancellationToken))
            .SelectMany(x => Samples(x.SamplePresetsJson).Select(sample => new
            {
                sample.Id, sample.Name, sample.ContributionTypeId, sample.ContributionTypeName,
                sample.Amount, sample.Currency, sample.TestMsisdn, sample.ExternalReferenceFormat,
                sample.PayerMessage, sample.PayeeNote, MtnSandboxScenarioId = x.Id,
                scenarioName = scenarioNames[x.Id], sample.IsActive, sample.IsSystemDefault,
                sample.ConfigurationVersion, sample.CreatedDate, sample.UpdatedDate
            })).OrderBy(x => x.Name).ToList();
        var contributionTypes = await db.ContributionTypes.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Code }).ToListAsync(cancellationToken);
        return new { scenarios, samples, contributionTypes, officialSource = OfficialSource };
    }

    public async Task<object> SaveScenarioAsync(MtnSandboxScenarioDraft draft, Guid actor, CancellationToken cancellationToken)
    {
        ValidateScenario(draft);
        var code = NormalizeCode(draft.ScenarioCode);
        var duplicate = await db.MtnSandboxScenarios.AnyAsync(x => x.ScenarioCode == code && (!draft.Id.HasValue || x.Id != draft.Id), cancellationToken);
        if (duplicate) throw new InvalidOperationException("A sandbox scenario with this code already exists.");
        var now = DateTime.UtcNow;
        var entity = draft.Id.HasValue
            ? await db.MtnSandboxScenarios.SingleOrDefaultAsync(x => x.Id == draft.Id, cancellationToken)
                ?? throw new KeyNotFoundException("Sandbox scenario not found.")
            : new MtnSandboxScenario { Id = Guid.NewGuid(), CreatedById = actor, CreatedDate = now };
        if (!draft.Id.HasValue) db.MtnSandboxScenarios.Add(entity);
        entity.ScenarioCode = code;
        entity.Name = Text(draft.Name, "Scenario name", 120);
        entity.Description = Text(draft.Description, "Scenario description", 500);
        entity.Msisdn = NormalizeSandboxMsisdn(draft.Msisdn);
        entity.ExpectedTransactionStatus = draft.ExpectedTransactionStatus.Trim().ToUpperInvariant();
        entity.ExpectedHttpResponseCode = draft.ExpectedHttpResponseCode;
        entity.ExpectedMtnErrorCode = Optional(draft.ExpectedMtnErrorCode, 100);
        entity.TestAmount = draft.TestAmount;
        entity.Currency = Currency(draft.Currency);
        entity.ExternalReferencePrefix = Text(draft.ExternalReferencePrefix, "External reference prefix", 40).ToUpperInvariant();
        entity.PayerMessage = Text(draft.PayerMessage, "Payer message", 160);
        entity.PayeeNote = Text(draft.PayeeNote, "Payee note", 160);
        entity.ExpectedProcessingBehaviour = draft.ExpectedProcessingBehaviour.Trim().ToUpperInvariant();
        entity.MaximumPollingAttempts = draft.MaximumPollingAttempts;
        entity.PollingIntervalSeconds = draft.PollingIntervalSeconds;
        entity.IsActive = draft.IsActive;
        entity.SourceClassification = draft.SourceClassification.Trim().ToUpperInvariant();
        entity.SourceReference = Optional(draft.SourceReference, 500);
        entity.ConfigurationVersion = draft.Id.HasValue ? entity.ConfigurationVersion + 1 : 1;
        entity.UpdatedById = actor; entity.UpdatedDate = now; entity.Version++;
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<object> DuplicateScenarioAsync(Guid id, Guid actor, CancellationToken cancellationToken)
    {
        var source = await db.MtnSandboxScenarios.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Sandbox scenario not found.");
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return await SaveScenarioAsync(new MtnSandboxScenarioDraft(null, $"CUSTOM-{suffix}", $"{source.Name} copy",
            source.Description, source.Msisdn, source.ExpectedTransactionStatus, source.ExpectedHttpResponseCode,
            source.ExpectedMtnErrorCode, source.TestAmount, source.Currency, source.ExternalReferencePrefix,
            source.PayerMessage, source.PayeeNote, source.ExpectedProcessingBehaviour,
            source.MaximumPollingAttempts, source.PollingIntervalSeconds, false, "CUSTOM", null), actor, cancellationToken);
    }

    public async Task DeleteScenarioAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.MtnSandboxScenarios.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Sandbox scenario not found.");
        if (entity.IsSystemDefault) throw new InvalidOperationException("Default scenarios can be disabled or edited, but not deleted. Use Restore Defaults to reset them.");
        if (await db.MtnSandboxTestExecutions.AnyAsync(x => x.MtnSandboxScenarioId == id, cancellationToken))
        {
            entity.IsActive = false; entity.UpdatedDate = DateTime.UtcNow; entity.Version++;
        }
        else db.MtnSandboxScenarios.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<object> SaveSampleAsync(MtnSandboxSampleDraft draft, Guid actor, CancellationToken cancellationToken)
    {
        if (draft.Amount <= 0 || draft.Amount > 1_000_000) throw new InvalidOperationException("Sample amount must be greater than zero and no more than 1,000,000.");
        var scenario = await db.MtnSandboxScenarios.SingleOrDefaultAsync(x => x.Id == draft.MtnSandboxScenarioId, cancellationToken)
            ?? throw new KeyNotFoundException("Sandbox scenario not found.");
        if (draft.ContributionTypeId.HasValue && !await db.ContributionTypes.AnyAsync(x => x.Id == draft.ContributionTypeId, cancellationToken))
            throw new KeyNotFoundException("Contribution type not found.");
        var name = Text(draft.Name, "Sample transaction name", 120);
        var allScenarios = await db.MtnSandboxScenarios.ToListAsync(cancellationToken);
        if (allScenarios.SelectMany(x => Samples(x.SamplePresetsJson)).Any(x =>
                x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && (!draft.Id.HasValue || x.Id != draft.Id)))
            throw new InvalidOperationException("A sample transaction with this name already exists.");
        var now = DateTime.UtcNow;
        var existing = allScenarios.SelectMany(x => Samples(x.SamplePresetsJson)).SingleOrDefault(x => x.Id == draft.Id);
        if (draft.Id.HasValue && existing is null) throw new KeyNotFoundException("Sample transaction not found.");
        if (existing is not null && existing.MtnSandboxScenarioId != scenario.Id)
        {
            var previousScenario = allScenarios.Single(x => x.Id == existing.MtnSandboxScenarioId);
            previousScenario.SamplePresetsJson = SerializeSamples(Samples(previousScenario.SamplePresetsJson).Where(x => x.Id != existing.Id));
        }
        var entity = new MtnSandboxSamplePreset(
            draft.Id ?? Guid.NewGuid(), name, draft.ContributionTypeId,
            Text(draft.ContributionTypeName, "Contribution type", 120), draft.Amount, Currency(draft.Currency),
            NormalizeSandboxMsisdn(draft.TestMsisdn), ValidateReferenceFormat(draft.ExternalReferenceFormat),
            Text(draft.PayerMessage, "Payer message", 160), Text(draft.PayeeNote, "Payee note", 160),
            scenario.Id, draft.IsActive, existing?.IsSystemDefault ?? false,
            (existing?.ConfigurationVersion ?? 0) + 1, existing?.CreatedDate ?? now, now);
        var target = Samples(scenario.SamplePresetsJson).Where(x => x.Id != entity.Id).Append(entity);
        scenario.SamplePresetsJson = SerializeSamples(target);
        scenario.UpdatedById = actor; scenario.UpdatedDate = now; scenario.Version++;
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<object> DuplicateSampleAsync(Guid id, Guid actor, CancellationToken cancellationToken)
    {
        var source = (await db.MtnSandboxScenarios.AsNoTracking().Select(x => x.SamplePresetsJson).ToListAsync(cancellationToken))
            .SelectMany(Samples).SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("Sample transaction not found.");
        return await SaveSampleAsync(new MtnSandboxSampleDraft(null, $"{source.Name} copy {Guid.NewGuid().ToString("N")[..4]}",
            source.ContributionTypeId, source.ContributionTypeName, source.Amount, source.Currency, source.TestMsisdn,
            source.ExternalReferenceFormat, source.PayerMessage, source.PayeeNote, source.MtnSandboxScenarioId, false), actor, cancellationToken);
    }

    public async Task DeleteSampleAsync(Guid id, CancellationToken cancellationToken)
    {
        var scenario = (await db.MtnSandboxScenarios.ToListAsync(cancellationToken))
            .SingleOrDefault(x => Samples(x.SamplePresetsJson).Any(sample => sample.Id == id))
            ?? throw new KeyNotFoundException("Sample transaction not found.");
        var entity = Samples(scenario.SamplePresetsJson).Single(x => x.Id == id);
        if (entity.IsSystemDefault) throw new InvalidOperationException("Default sample transactions can be edited, but not deleted. Use Restore Defaults to reset them.");
        scenario.SamplePresetsJson = SerializeSamples(Samples(scenario.SamplePresetsJson).Where(x => x.Id != id));
        scenario.UpdatedDate = DateTime.UtcNow; scenario.Version++;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<object> RestoreDefaultsAsync(Guid actor, CancellationToken cancellationToken)
    {
        await UpsertDefaultsAsync(actor, reset: true, cancellationToken);
        return new { message = "MTN's documented sandbox scenarios and the inferred eMilimo success presets were restored." };
    }

    public async Task<object> PreviewAsync(MtnSandboxExecutionRequest request, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(request, cancellationToken);
        return RequestPreview(resolved, Guid.NewGuid());
    }

    public async Task<object> ExecuteAsync(MtnSandboxExecutionRequest request, Guid actor, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(request, cancellationToken);
        if (!resolved.Configuration.IsValidated)
            throw new InvalidOperationException("Validate the sandbox provider connection before executing a scenario.");
        var transactionUuid = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var execution = new MtnSandboxTestExecution
        {
            Id = Guid.NewGuid(), PaymentProviderConfigurationId = resolved.Configuration.Id,
            MtnSandboxScenarioId = resolved.Scenario.Id, SamplePresetId = resolved.Sample?.Id,
            SamplePresetSnapshotJson = resolved.Sample is null ? null : JsonSerializer.Serialize(resolved.Sample),
            TransactionUuid = transactionUuid, ExternalReference = ExternalReference(resolved, transactionUuid),
            TestMsisdn = resolved.Msisdn, Amount = resolved.Amount, Currency = resolved.Currency,
            ExpectedStatus = resolved.Scenario.ExpectedTransactionStatus,
            ExpectedErrorCode = resolved.Scenario.ExpectedMtnErrorCode,
            ExpectedHttpStatusCode = resolved.Scenario.ExpectedHttpResponseCode,
            ActualStatus = PaymentTransactionStatuses.Created, Verdict = "PENDING", ExecutionStatus = "CREATED",
            ProviderConfigurationVersion = resolved.Configuration.ConfigurationVersion,
            MaximumPollingAttempts = resolved.Scenario.MaximumPollingAttempts,
            PollingIntervalSeconds = resolved.Scenario.PollingIntervalSeconds,
            StartedAtUtc = now, CorrelationId = CorrelationId(),
            SanitizedRequestPreview = JsonSerializer.Serialize(RequestPreview(resolved, transactionUuid)),
            TimelineJson = SerializeTimeline([new("CREATED", "Sandbox execution created.", now)]),
            CreatedById = actor, UpdatedById = actor, CreatedDate = now, UpdatedDate = now, Version = 1
        };
        db.MtnSandboxTestExecutions.Add(execution);
        await db.SaveChangesAsync(cancellationToken);

        var runtime = new Dictionary<string, string>(await configurations.GetRuntimeSettingsAsync(resolved.Configuration.Id, false, cancellationToken), StringComparer.OrdinalIgnoreCase)
        {
            ["PayerMessage"] = resolved.PayerMessage,
            ["PayeeNote"] = resolved.PayeeNote
        };
        var callback = Callback(runtime);
        try
        {
            var provider = providers.GetRequired(resolved.Configuration.ProviderKey);
            var result = await provider.InitiateAsync(new PaymentInitiationRequest(transactionUuid,
                execution.ExternalReference, "MTN_MOMO", resolved.Amount, resolved.Currency, resolved.Msisdn,
                callback, $"sandbox-test:{execution.Id:N}", runtime), cancellationToken);
            execution.ActualHttpStatusCode = result.Status == PaymentTransactionStatuses.Pending ? 202 : null;
            execution.ActualStatus = result.Status;
            execution.ProviderMessage = result.ProviderMessage;
            execution.ExecutionStatus = result.Status == PaymentTransactionStatuses.Pending ? "ACCEPTED" : "INITIATED";
            execution.NextPollAtUtc = DateTime.UtcNow.AddSeconds(execution.PollingIntervalSeconds);
            Append(execution, execution.ActualHttpStatusCode == 202 ? "202_ACCEPTED" : "INITIATED", result.ProviderMessage);
            Evaluate(execution, final: false);
        }
        catch (PaymentProviderException ex)
        {
            execution.ActualHttpStatusCode = ex.HttpStatusCode;
            execution.ActualErrorCode = ex.ProviderCode ?? ex.ErrorCategory;
            execution.ActualStatus = ex.IsAmbiguous ? PaymentTransactionStatuses.Unknown : PaymentTransactionStatuses.Failed;
            execution.ProviderMessage = ex.Message;
            execution.ExecutionStatus = ex.IsAmbiguous ? "RECONCILIATION_REQUIRED" : "COMPLETED";
            execution.CompletedAtUtc = ex.IsAmbiguous ? null : DateTime.UtcNow;
            Append(execution, ex.IsAmbiguous ? "AMBIGUOUS_RESPONSE" : "REQUEST_FAILED", ex.Message);
            Evaluate(execution, final: !ex.IsAmbiguous);
        }
        execution.UpdatedById = actor; execution.UpdatedDate = DateTime.UtcNow; execution.Version++;
        await db.SaveChangesAsync(cancellationToken);
        await LinkDiagnosticsAsync(execution, cancellationToken);
        return await DetailAsync(execution.Id, cancellationToken);
    }

    public async Task<object> PollAsync(Guid executionId, Guid actor, CancellationToken cancellationToken)
    {
        var execution = await db.MtnSandboxTestExecutions.Include(x => x.PaymentProviderConfiguration)
            .SingleOrDefaultAsync(x => x.Id == executionId, cancellationToken)
            ?? throw new KeyNotFoundException("Sandbox execution not found.");
        if (!execution.PaymentProviderConfiguration!.Environment.Equals("SANDBOX", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only sandbox executions can be queried from Developer Mode.");
        if (execution.PollingAttemptCount >= execution.MaximumPollingAttempts)
            throw new InvalidOperationException("This execution has reached its configured polling limit.");
        var runtime = await configurations.GetRuntimeSettingsAsync(execution.PaymentProviderConfigurationId, false, cancellationToken);
        try
        {
            var result = await providers.GetRequired(execution.PaymentProviderConfiguration.ProviderKey)
                .QueryStatusAsync(execution.TransactionUuid.ToString("D"), runtime, cancellationToken);
            execution.PollingAttemptCount++;
            execution.ActualHttpStatusCode ??= 200;
            execution.ActualStatus = result.Status;
            execution.ProviderMessage = result.ProviderMessage;
            execution.ActualErrorCode = ExtractErrorCode(result.ProviderMessage);
            var final = IsTerminal(result.Status);
            execution.ExecutionStatus = final ? "COMPLETED" : "POLLING";
            execution.NextPollAtUtc = final ? null : DateTime.UtcNow.AddSeconds(execution.PollingIntervalSeconds);
            execution.CompletedAtUtc = final ? DateTime.UtcNow : null;
            Append(execution, "STATUS_CHECK", $"Attempt {execution.PollingAttemptCount}: {result.Status}. {result.ProviderMessage}".Trim());
            if (final) Append(execution, result.Status, "Provider returned a terminal sandbox status.");
            Evaluate(execution, final);
        }
        catch (PaymentProviderException ex)
        {
            execution.PollingAttemptCount++;
            execution.ActualHttpStatusCode ??= ex.HttpStatusCode;
            execution.ActualErrorCode = ex.ProviderCode ?? ex.ErrorCategory;
            execution.ProviderMessage = ex.Message;
            execution.ExecutionStatus = execution.PollingAttemptCount >= execution.MaximumPollingAttempts ? "COMPLETED" : "POLLING";
            execution.NextPollAtUtc = execution.ExecutionStatus == "COMPLETED" ? null : DateTime.UtcNow.AddSeconds(execution.PollingIntervalSeconds);
            Append(execution, "STATUS_ERROR", ex.Message);
            Evaluate(execution, execution.ExecutionStatus == "COMPLETED");
        }
        if (execution.PollingAttemptCount >= execution.MaximumPollingAttempts && !IsTerminal(execution.ActualStatus))
        {
            execution.ExecutionStatus = "COMPLETED"; execution.CompletedAtUtc = DateTime.UtcNow; execution.NextPollAtUtc = null;
            Append(execution, "POLLING_LIMIT_REACHED", "The configured maximum polling attempts was reached.");
            Evaluate(execution, true);
        }
        execution.UpdatedById = actor; execution.UpdatedDate = DateTime.UtcNow; execution.Version++;
        await db.SaveChangesAsync(cancellationToken);
        await LinkDiagnosticsAsync(execution, cancellationToken);
        return await DetailAsync(execution.Id, cancellationToken);
    }

    public async Task<object> ExecutionsAsync(Guid? scenarioId, string? status, string? reference, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.MtnSandboxTestExecutions.AsNoTracking().AsQueryable();
        if (scenarioId.HasValue) query = query.Where(x => x.MtnSandboxScenarioId == scenarioId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.ActualStatus == status.Trim().ToUpper());
        if (!string.IsNullOrWhiteSpace(reference)) query = query.Where(x => x.ExternalReference.Contains(reference) || x.TransactionUuid.ToString().Contains(reference));
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.StartedAtUtc).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.TransactionUuid, x.ExternalReference, scenarioName = x.MtnSandboxScenario!.Name,
                x.SamplePresetSnapshotJson, x.TestMsisdn, x.Amount, x.Currency, x.ExpectedStatus, x.ActualStatus,
                x.ActualHttpStatusCode, x.ActualErrorCode, x.Verdict, x.ExecutionStatus, x.PollingAttemptCount,
                x.MaximumPollingAttempts, x.StartedAtUtc, x.CompletedAtUtc }).ToListAsync(cancellationToken);
        var items = rows.Select(x => new { x.Id, x.TransactionUuid, x.ExternalReference, x.scenarioName,
            sampleName = Sample(x.SamplePresetSnapshotJson)?.Name, x.TestMsisdn, x.Amount, x.Currency,
            x.ExpectedStatus, x.ActualStatus, x.ActualHttpStatusCode, x.ActualErrorCode, x.Verdict,
            x.ExecutionStatus, x.PollingAttemptCount, x.MaximumPollingAttempts, x.StartedAtUtc, x.CompletedAtUtc }).ToList();
        return new { items, total, page, pageSize };
    }

    public async Task<object> DetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var execution = await db.MtnSandboxTestExecutions.AsNoTracking()
            .Include(x => x.MtnSandboxScenario)
            .Include(x => x.PaymentProviderConfiguration).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Sandbox execution not found.");
        var diagnostics = await db.PaymentApiExchanges.AsNoTracking().Where(x => x.MtnSandboxTestExecutionId == id ||
            (x.PaymentProviderConfigurationId == execution.PaymentProviderConfigurationId && x.TransactionReference == execution.TransactionUuid.ToString("D")))
            .OrderBy(x => x.RequestedAtUtc).Select(x => new { x.Id, x.Operation, x.HttpMethod, x.Endpoint, x.HttpStatusCode,
                x.DurationMilliseconds, x.ErrorCategory, x.RequestedAtUtc }).ToListAsync(cancellationToken);
        return new
        {
            execution.Id, execution.TransactionUuid, execution.ExternalReference,
            scenarioId = execution.MtnSandboxScenarioId, scenarioName = execution.MtnSandboxScenario!.Name,
            sampleId = execution.SamplePresetId, sampleName = Sample(execution.SamplePresetSnapshotJson)?.Name,
            execution.TestMsisdn, execution.Amount, execution.Currency, execution.ExpectedStatus,
            execution.ExpectedErrorCode, execution.ExpectedHttpStatusCode, execution.ActualStatus,
            execution.ActualErrorCode, execution.ActualHttpStatusCode, execution.ProviderMessage,
            execution.Verdict, execution.ExecutionStatus, execution.ProviderConfigurationVersion,
            execution.PollingAttemptCount, execution.MaximumPollingAttempts, execution.PollingIntervalSeconds,
            execution.NextPollAtUtc, execution.StartedAtUtc, execution.CompletedAtUtc,
            requestPreview = JsonSerializer.Deserialize<JsonElement>(execution.SanitizedRequestPreview),
            timeline = JsonSerializer.Deserialize<TimelineEntry[]>(execution.TimelineJson) ?? [], diagnostics
        };
    }

    private async Task<ResolvedTest> ResolveAsync(MtnSandboxExecutionRequest request, CancellationToken cancellationToken)
    {
        var configuration = await db.PaymentProviderConfigurations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.ConfigurationId, cancellationToken)
            ?? throw new KeyNotFoundException("Payment provider configuration not found.");
        if (configuration.ProviderKey != MtnMomoProfile.ProviderKey || !configuration.Environment.Equals("SANDBOX", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Sandbox testing requires an MTN MoMo SANDBOX profile.");
        var scenario = await db.MtnSandboxScenarios.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.ScenarioId && x.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Active sandbox scenario not found.");
        MtnSandboxSamplePreset? sample = null;
        if (request.SampleTransactionId.HasValue)
            sample = Samples(scenario.SamplePresetsJson).SingleOrDefault(x => x.Id == request.SampleTransactionId && x.IsActive)
                ?? throw new KeyNotFoundException("Active sample transaction not found for the selected scenario.");
        var amount = request.Amount ?? sample?.Amount ?? scenario.TestAmount;
        if (amount <= 0 || amount > 1_000_000) throw new InvalidOperationException("Sandbox amount must be greater than zero and no more than 1,000,000.");
        var currency = Currency(request.Currency ?? sample?.Currency ?? scenario.Currency);
        if (currency != "EUR") throw new InvalidOperationException("MTN sandbox scenarios must use EUR.");
        var msisdn = NormalizeSandboxMsisdn(request.Msisdn ?? sample?.TestMsisdn ?? scenario.Msisdn);
        return new ResolvedTest(configuration, scenario, sample, amount, currency, msisdn,
            Text(request.ExternalReferencePrefix ?? scenario.ExternalReferencePrefix, "External reference prefix", 40),
            Text(request.PayerMessage ?? sample?.PayerMessage ?? scenario.PayerMessage, "Payer message", 160),
            Text(request.PayeeNote ?? sample?.PayeeNote ?? scenario.PayeeNote, "Payee note", 160));
    }

    private async Task EnsureDefaultsAsync(CancellationToken cancellationToken)
    {
        if (!await db.MtnSandboxScenarios.AnyAsync(cancellationToken))
            await UpsertDefaultsAsync(null, reset: false, cancellationToken);
    }

    private async Task UpsertDefaultsAsync(Guid? actor, bool reset, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        foreach (var definition in Defaults())
        {
            var entity = await db.MtnSandboxScenarios.SingleOrDefaultAsync(x => x.ScenarioCode == definition.Code, cancellationToken);
            if (entity is null)
            {
                entity = new MtnSandboxScenario { Id = definition.Id, ScenarioCode = definition.Code, IsSystemDefault = true,
                    CreatedById = actor, CreatedDate = now, Version = 1 };
                db.MtnSandboxScenarios.Add(entity);
            }
            else if (!reset) continue;
            entity.Name = definition.Name; entity.Description = definition.Description; entity.Msisdn = definition.Msisdn;
            entity.ExpectedTransactionStatus = definition.Status; entity.ExpectedHttpResponseCode = definition.Http;
            entity.ExpectedMtnErrorCode = definition.Error; entity.TestAmount = 1m; entity.Currency = "EUR";
            entity.ExternalReferencePrefix = "EMILIMO-SBX"; entity.PayerMessage = "eMilimo MTN sandbox test";
            entity.PayeeNote = definition.Name; entity.ExpectedProcessingBehaviour = definition.Behaviour;
            entity.MaximumPollingAttempts = definition.Behaviour == "DELAYED" ? 20 : 12; entity.PollingIntervalSeconds = 10;
            entity.IsActive = true; entity.SourceClassification = definition.Source; entity.SourceReference = definition.Source == "DOCUMENTED" ? OfficialSource : null;
            entity.IsSystemDefault = true; entity.ConfigurationVersion = entity.ConfigurationVersion + (reset ? 1 : 0);
            entity.UpdatedById = actor; entity.UpdatedDate = now;
        }
        await db.SaveChangesAsync(cancellationToken);

        var types = await db.ContributionTypes.AsNoTracking().ToDictionaryAsync(x => x.Name.ToUpper(), x => x.Id, cancellationToken);
        var samples = new[]
        {
            (Id: Guid.Parse("9ab579b1-8278-4c6f-bf27-2f51207f7001"), Name: "Tithe EUR 100", Type: "Tithe", Amount: 100m, Msisdn: "46733123465"),
            (Id: Guid.Parse("9ab579b1-8278-4c6f-bf27-2f51207f7002"), Name: "Offering EUR 50", Type: "Offering", Amount: 50m, Msisdn: "46733123466"),
            (Id: Guid.Parse("9ab579b1-8278-4c6f-bf27-2f51207f7003"), Name: "Building Fund EUR 500", Type: "Building Fund", Amount: 500m, Msisdn: "46733123467"),
            (Id: Guid.Parse("9ab579b1-8278-4c6f-bf27-2f51207f7004"), Name: "General Donation EUR 250", Type: "General Donation", Amount: 250m, Msisdn: "46733123468")
        };
        foreach (var definition in samples)
        {
            var scenario = await db.MtnSandboxScenarios.SingleAsync(x => x.Msisdn == definition.Msisdn, cancellationToken);
            var existing = Samples(scenario.SamplePresetsJson).SingleOrDefault(x => x.Id == definition.Id);
            if (existing is not null && !reset) continue;
            var preset = new MtnSandboxSamplePreset(definition.Id, definition.Name,
                types.TryGetValue(definition.Type.ToUpper(), out var contributionTypeId) ? contributionTypeId : null,
                definition.Type, definition.Amount, "EUR", definition.Msisdn, "{prefix}-{uuid}",
                $"eMilimo {definition.Type} sandbox test", definition.Type, scenario.Id, true, true,
                (existing?.ConfigurationVersion ?? 1) + (existing is not null && reset ? 1 : 0), existing?.CreatedDate ?? now, now);
            scenario.SamplePresetsJson = SerializeSamples(Samples(scenario.SamplePresetsJson).Where(x => x.Id != preset.Id).Append(preset));
            scenario.UpdatedById = actor; scenario.UpdatedDate = now; scenario.Version++;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static DefaultScenario[] Defaults() =>
    [
        D("46733123450", "PAYER_FAILED", "PayerFailed", PaymentTransactionStatuses.Failed, "PAYER_FAILED", "TERMINAL_FAILURE"),
        D("46733123451", "PAYER_REJECTED", "PayerRejected", PaymentTransactionStatuses.Failed, "PAYER_REJECTED", "TERMINAL_FAILURE"),
        D("46733123452", "PAYER_EXPIRED", "PayerExpired", PaymentTransactionStatuses.Expired, "PAYER_EXPIRED", "TERMINAL_FAILURE"),
        D("46733123453", "PAYER_ONGOING", "PayerOngoing", PaymentTransactionStatuses.Pending, null, "PENDING"),
        D("46733123454", "PAYER_DELAYED", "PayerDelayed", PaymentTransactionStatuses.Pending, null, "DELAYED"),
        D("46733123455", "PAYER_NOT_FOUND", "PayerNotFound", PaymentTransactionStatuses.Failed, "PAYER_NOT_FOUND", "TERMINAL_FAILURE"),
        D("46733123456", "PAYEE_NOT_ALLOWED", "PayeeNotAllowedToReceive", PaymentTransactionStatuses.Failed, "PAYEE_NOT_ALLOWED_TO_RECEIVE", "TERMINAL_FAILURE"),
        D("46733123457", "PAYER_NOT_ALLOWED", "PayerNotAllowed", PaymentTransactionStatuses.Failed, "PAYER_NOT_ALLOWED", "TERMINAL_FAILURE"),
        D("46733123458", "TARGET_ENVIRONMENT_INVALID", "NotAllowedTargetEnvironment", PaymentTransactionStatuses.Failed, "NOT_ALLOWED_TARGET_ENVIRONMENT", "TERMINAL_FAILURE"),
        D("46733123459", "CALLBACK_HOST_INVALID", "InvalidCallbackUrlHost", PaymentTransactionStatuses.Failed, "INVALID_CALLBACK_URL_HOST", "REQUEST_ERROR"),
        D("46733123460", "CURRENCY_INVALID", "InvalidCurrency", PaymentTransactionStatuses.Failed, "INVALID_CURRENCY", "REQUEST_ERROR"),
        D("46733123461", "INTERNAL_PROCESSING_ERROR", "InternalProcessingError", PaymentTransactionStatuses.Failed, "INTERNAL_PROCESSING_ERROR", "TERMINAL_FAILURE"),
        D("46733123462", "SERVICE_UNAVAILABLE", "ServiceUnavailable", PaymentTransactionStatuses.Failed, "SERVICE_UNAVAILABLE", "TERMINAL_FAILURE"),
        D("46733123463", "COULD_NOT_PERFORM", "CouldNotPerformTransaction", PaymentTransactionStatuses.Failed, "COULD_NOT_PERFORM_TRANSACTION", "TERMINAL_FAILURE"),
        D("46733123464", "TRANSFER_TYPE_UNKNOWN", "TransfertypeUnknown", PaymentTransactionStatuses.Failed, "TRANSFER_TYPE_UNKNOWN", "TERMINAL_FAILURE"),
        I("46733123465", "DEFAULT_SUCCESS_65"), I("46733123466", "DEFAULT_SUCCESS_66"),
        I("46733123467", "DEFAULT_SUCCESS_67"), I("46733123468", "DEFAULT_SUCCESS_68")
    ];

    private static DefaultScenario D(string msisdn, string code, string name, string status, string? error, string behaviour) =>
        new(StableId(msisdn), code, name, $"MTN documented Request to Pay sandbox scenario: {name}.", msisdn, status, null, error, behaviour, "DOCUMENTED");
    private static DefaultScenario I(string msisdn, string code) =>
        new(StableId(msisdn), code, "Default Success", "Editable eMilimo success preset based on MTN sandbox default-success behaviour; this individual number is not represented as a certified fixture.", msisdn,
            PaymentTransactionStatuses.Succeeded, 202, null, "SUCCESS", "INFERRED");
    private static Guid StableId(string msisdn) => Guid.Parse($"a7e8d326-56ab-4f91-9c01-{msisdn.PadLeft(12, '0')[^12..]}");

    private static object RequestPreview(ResolvedTest resolved, Guid uuid) => new
    {
        method = "POST", endpoint = "/collection/v1_0/requesttopay", referenceId = uuid,
        targetEnvironment = "sandbox", amount = resolved.Amount.ToString("0.00"), currency = resolved.Currency,
        externalReference = ExternalReference(resolved, uuid), payer = new { partyIdType = "MSISDN", partyId = resolved.Msisdn },
        payerMessage = resolved.PayerMessage, payeeNote = resolved.PayeeNote,
        note = "Authorization and subscription credentials are added only by the backend and are never returned."
    };

    private static string ExternalReference(ResolvedTest resolved, Guid uuid)
    {
        var format = resolved.Sample?.ExternalReferenceFormat ?? "{prefix}-{uuid}";
        var reference = format.Replace("{prefix}", resolved.Prefix, StringComparison.OrdinalIgnoreCase)
            .Replace("{uuid}", uuid.ToString("N"), StringComparison.OrdinalIgnoreCase);
        return reference.Length <= 120 ? reference : reference[..120];
    }

    private static Uri Callback(IReadOnlyDictionary<string, string> settings) =>
        settings.TryGetValue("CallbackUrl", out var value) && Uri.TryCreate(value, UriKind.Absolute, out var callback)
            ? callback : new Uri("https://sandbox-callback.invalid/api/public/digital-giving/webhooks/MTN_MOMO_DIRECT");

    private async Task LinkDiagnosticsAsync(MtnSandboxTestExecution execution, CancellationToken cancellationToken)
    {
        var reference = execution.TransactionUuid.ToString("D");
        var exchanges = await db.PaymentApiExchanges.Where(x => x.PaymentProviderConfigurationId == execution.PaymentProviderConfigurationId &&
                x.TransactionReference == reference && x.MtnSandboxTestExecutionId == null).ToListAsync(cancellationToken);
        foreach (var exchange in exchanges) exchange.MtnSandboxTestExecutionId = execution.Id;
        if (exchanges.Count > 0) await db.SaveChangesAsync(cancellationToken);
    }

    private static void Evaluate(MtnSandboxTestExecution execution, bool final)
    {
        var statusMatches = execution.ActualStatus.Equals(execution.ExpectedStatus, StringComparison.OrdinalIgnoreCase);
        var errorMatches = string.IsNullOrWhiteSpace(execution.ExpectedErrorCode) ||
            (!string.IsNullOrWhiteSpace(execution.ActualErrorCode) && NormalizeError(execution.ActualErrorCode).Contains(NormalizeError(execution.ExpectedErrorCode))) ||
            (!string.IsNullOrWhiteSpace(execution.ProviderMessage) && NormalizeError(execution.ProviderMessage).Contains(NormalizeError(execution.ExpectedErrorCode)));
        var httpMatches = !execution.ExpectedHttpStatusCode.HasValue || execution.ExpectedHttpStatusCode == execution.ActualHttpStatusCode;
        execution.Verdict = !final && execution.PollingAttemptCount == 0
            ? "PENDING"
            : statusMatches && errorMatches && httpMatches ? "PASSED" : final ? "FAILED" : "PENDING";
        if (execution.Verdict != "PENDING") Append(execution, $"TEST_{execution.Verdict}", "Actual provider results were compared with the saved scenario expectations.");
    }

    private static void Append(MtnSandboxTestExecution execution, string state, string? message)
    {
        var items = JsonSerializer.Deserialize<List<TimelineEntry>>(execution.TimelineJson) ?? [];
        items.Add(new TimelineEntry(state, message ?? string.Empty, DateTime.UtcNow));
        execution.TimelineJson = SerializeTimeline(items);
    }
    private static string SerializeTimeline(IEnumerable<TimelineEntry> items) => JsonSerializer.Serialize(items);
    private static bool IsTerminal(string status) => status is PaymentTransactionStatuses.Succeeded or PaymentTransactionStatuses.Failed or PaymentTransactionStatuses.Cancelled or PaymentTransactionStatuses.Expired;
    private static string? ExtractErrorCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Length <= 100 ? value : value[..100];
    private static string NormalizeError(string value) => Regex.Replace(value.ToUpperInvariant(), "[^A-Z0-9]", string.Empty);
    private string? CorrelationId() => httpContext.HttpContext?.TraceIdentifier;

    private static void ValidateScenario(MtnSandboxScenarioDraft draft)
    {
        if (!AllowedStatuses.Contains(draft.ExpectedTransactionStatus.Trim().ToUpperInvariant())) throw new InvalidOperationException("Expected transaction status is not supported.");
        if (!AllowedBehaviours.Contains(draft.ExpectedProcessingBehaviour.Trim().ToUpperInvariant())) throw new InvalidOperationException("Expected processing behaviour is not supported.");
        if (!AllowedSources.Contains(draft.SourceClassification.Trim().ToUpperInvariant())) throw new InvalidOperationException("Source classification must be DOCUMENTED, INFERRED or CUSTOM.");
        if (draft.TestAmount <= 0 || draft.TestAmount > 1_000_000) throw new InvalidOperationException("Test amount must be greater than zero and no more than 1,000,000.");
        if (draft.MaximumPollingAttempts is < 1 or > 100) throw new InvalidOperationException("Maximum polling attempts must be between 1 and 100.");
        if (draft.PollingIntervalSeconds is < 1 or > 3600) throw new InvalidOperationException("Polling interval must be between 1 and 3,600 seconds.");
        if (draft.ExpectedHttpResponseCode is < 100 or > 599) throw new InvalidOperationException("Expected HTTP response code must be between 100 and 599.");
        _ = Currency(draft.Currency); _ = NormalizeSandboxMsisdn(draft.Msisdn);
    }

    private static string NormalizeCode(string value)
    {
        var code = value.Trim().ToUpperInvariant().Replace(' ', '_');
        if (!CodePattern.IsMatch(code)) throw new InvalidOperationException("Scenario code must contain 3-80 uppercase letters, numbers, underscores or hyphens.");
        return code;
    }
    private static string Currency(string value)
    {
        var currency = value.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(currency, "^[A-Z]{3}$")) throw new InvalidOperationException("Currency must be a three-letter ISO code.");
        return currency;
    }
    private static string NormalizeSandboxMsisdn(string value)
    {
        var msisdn = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (msisdn.Length is < 8 or > 15) throw new InvalidOperationException("Sandbox MSISDN must contain 8 to 15 digits.");
        return msisdn;
    }
    private static string ValidateReferenceFormat(string value)
    {
        var format = Text(value, "External reference format", 120);
        if (!format.Contains("{uuid}", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("External reference format must contain {uuid}.");
        return format;
    }
    private static string Text(string? value, string label, int maximum)
    {
        var result = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(result)) throw new InvalidOperationException($"{label} is required.");
        if (result.Length > maximum) throw new InvalidOperationException($"{label} cannot exceed {maximum} characters.");
        if (result.Any(char.IsControl)) throw new InvalidOperationException($"{label} contains unsupported control characters.");
        return result;
    }
    private static string? Optional(string? value, int maximum) => string.IsNullOrWhiteSpace(value) ? null : Text(value, "Value", maximum);

    private sealed record DefaultScenario(Guid Id, string Code, string Name, string Description, string Msisdn,
        string Status, int? Http, string? Error, string Behaviour, string Source);
    private sealed record ResolvedTest(PaymentProviderConfiguration Configuration, MtnSandboxScenario Scenario,
        MtnSandboxSamplePreset? Sample, decimal Amount, string Currency, string Msisdn, string Prefix,
        string PayerMessage, string PayeeNote);
    private static List<MtnSandboxSamplePreset> Samples(string? json) =>
        string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<MtnSandboxSamplePreset>>(json) ?? [];
    private static MtnSandboxSamplePreset? Sample(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<MtnSandboxSamplePreset>(json);
    private static string SerializeSamples(IEnumerable<MtnSandboxSamplePreset> samples) => JsonSerializer.Serialize(samples.OrderBy(x => x.Name));
    public sealed record TimelineEntry(string State, string Message, DateTime OccurredAtUtc);
}
