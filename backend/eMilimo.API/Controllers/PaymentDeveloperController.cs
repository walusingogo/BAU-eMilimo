using eMilimo.API.Authorization;
using eMilimo.API.Payments;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Controllers;

[ApiController]
[Route("api/digital-giving/developer")]
[Authorize]
[RequirePermission("payments.developer.manage")]
public sealed class PaymentDeveloperController(
    AppDbContext db,
    PaymentDeveloperService developer,
    MtnSandboxTestingService sandbox,
    ISystemLogWriter logs,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("sandbox/catalog")]
    public async Task<IActionResult> SandboxCatalog(CancellationToken cancellationToken) =>
        Ok(await sandbox.CatalogAsync(cancellationToken));

    [HttpPost("sandbox/scenarios")]
    public async Task<IActionResult> SaveScenario(MtnSandboxScenarioDraft request, CancellationToken cancellationToken)
    {
        var result = await sandbox.SaveScenarioAsync(request, Actor(), cancellationToken);
        await AuditAsync(request.Id.HasValue ? "ScenarioUpdated" : "ScenarioCreated", $"Scenario {request.ScenarioCode} was saved.", cancellationToken);
        return Ok(result);
    }

    [HttpPost("sandbox/scenarios/{id:guid}/duplicate")]
    public async Task<IActionResult> DuplicateScenario(Guid id, CancellationToken cancellationToken)
    {
        var result = await sandbox.DuplicateScenarioAsync(id, Actor(), cancellationToken);
        await AuditAsync("ScenarioDuplicated", $"Scenario {id} was duplicated.", cancellationToken);
        return Ok(result);
    }

    [HttpDelete("sandbox/scenarios/{id:guid}")]
    public async Task<IActionResult> DeleteScenario(Guid id, CancellationToken cancellationToken)
    {
        await sandbox.DeleteScenarioAsync(id, cancellationToken);
        await AuditAsync("ScenarioDeletedOrDisabled", $"Scenario {id} was deleted or disabled.", cancellationToken);
        return NoContent();
    }

    [HttpPost("sandbox/samples")]
    public async Task<IActionResult> SaveSample(MtnSandboxSampleDraft request, CancellationToken cancellationToken)
    {
        var result = await sandbox.SaveSampleAsync(request, Actor(), cancellationToken);
        await AuditAsync(request.Id.HasValue ? "SampleUpdated" : "SampleCreated", $"Sample transaction {request.Name} was saved.", cancellationToken);
        return Ok(result);
    }

    [HttpPost("sandbox/samples/{id:guid}/duplicate")]
    public async Task<IActionResult> DuplicateSample(Guid id, CancellationToken cancellationToken)
    {
        var result = await sandbox.DuplicateSampleAsync(id, Actor(), cancellationToken);
        await AuditAsync("SampleDuplicated", $"Sample transaction {id} was duplicated.", cancellationToken);
        return Ok(result);
    }

    [HttpDelete("sandbox/samples/{id:guid}")]
    public async Task<IActionResult> DeleteSample(Guid id, CancellationToken cancellationToken)
    {
        await sandbox.DeleteSampleAsync(id, cancellationToken);
        await AuditAsync("SampleDeletedOrDisabled", $"Sample transaction {id} was deleted or disabled.", cancellationToken);
        return NoContent();
    }

    [HttpPost("sandbox/restore-defaults")]
    public async Task<IActionResult> RestoreSandboxDefaults(CancellationToken cancellationToken)
    {
        var result = await sandbox.RestoreDefaultsAsync(Actor(), cancellationToken);
        await AuditAsync("DefaultsRestored", "MTN sandbox defaults were restored.", cancellationToken);
        return Ok(result);
    }

    [HttpPost("sandbox/preview")]
    public async Task<IActionResult> PreviewSandboxRequest(MtnSandboxExecutionRequest request, CancellationToken cancellationToken) =>
        Ok(await sandbox.PreviewAsync(request, cancellationToken));

    [HttpPost("sandbox/execute")]
    public async Task<IActionResult> ExecuteSandboxRequest(MtnSandboxExecutionRequest request, CancellationToken cancellationToken)
    {
        var result = await sandbox.ExecuteAsync(request, Actor(), cancellationToken);
        await AuditAsync("SandboxExecuted", $"MTN sandbox scenario {request.ScenarioId} was executed using configuration {request.ConfigurationId}.", cancellationToken);
        return Ok(result);
    }

    [HttpPost("sandbox/executions/{id:guid}/poll")]
    public async Task<IActionResult> PollSandboxExecution(Guid id, CancellationToken cancellationToken) =>
        Ok(await sandbox.PollAsync(id, Actor(), cancellationToken));

    [HttpGet("sandbox/executions")]
    public async Task<IActionResult> SandboxExecutions([FromQuery] Guid? scenarioId, [FromQuery] string? status,
        [FromQuery] string? reference, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await sandbox.ExecutionsAsync(scenarioId, status, reference, page, pageSize, cancellationToken));

    [HttpGet("sandbox/executions/{id:guid}")]
    public async Task<IActionResult> SandboxExecution(Guid id, CancellationToken cancellationToken) =>
        Ok(await sandbox.DetailAsync(id, cancellationToken));

    [HttpPost("configurations/{id:guid}/test-connection")]
    public async Task<IActionResult> TestConnection(Guid id, CancellationToken cancellationToken) =>
        Ok(await developer.TestConnectionAsync(id, Actor(), cancellationToken));

    [HttpPost("configurations/{id:guid}/sandbox-payment")]
    public async Task<IActionResult> SandboxPayment(Guid id, SandboxPaymentTestRequest request, CancellationToken cancellationToken) =>
        Ok(await developer.SandboxPaymentAsync(id, request, cancellationToken));

    [HttpGet("configurations/{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken cancellationToken) =>
        Ok(await developer.HistoryAsync(id, cancellationToken));

    [HttpPost("configurations/{id:guid}/restore/{version:int}")]
    public async Task<IActionResult> Restore(Guid id, int version, RestoreRequest request, CancellationToken cancellationToken) =>
        Ok(await developer.RestoreAsync(id, version, request.Reason, Actor(), cancellationToken));

    [HttpGet("diagnostics")]
    public async Task<IActionResult> Diagnostics([FromQuery] Guid? configurationId, [FromQuery] Guid? scenarioId,
        [FromQuery] string? reference, [FromQuery] string? endpoint,
        [FromQuery] string? environment, [FromQuery] int? httpStatus, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.PaymentApiExchanges.AsNoTracking().Include(x => x.PaymentProviderConfiguration).AsQueryable();
        if (configurationId.HasValue) query = query.Where(x => x.PaymentProviderConfigurationId == configurationId);
        if (scenarioId.HasValue) query = query.Where(x => x.MtnSandboxTestExecution != null && x.MtnSandboxTestExecution.MtnSandboxScenarioId == scenarioId);
        if (!string.IsNullOrWhiteSpace(reference)) query = query.Where(x => x.TransactionReference != null && x.TransactionReference.Contains(reference));
        if (!string.IsNullOrWhiteSpace(endpoint)) query = query.Where(x => x.Endpoint.Contains(endpoint));
        if (!string.IsNullOrWhiteSpace(environment)) query = query.Where(x => x.Environment == environment.Trim().ToUpper());
        if (httpStatus.HasValue) query = query.Where(x => x.HttpStatusCode == httpStatus);
        if (from.HasValue) query = query.Where(x => x.RequestedAtUtc >= DateTime.SpecifyKind(from.Value, DateTimeKind.Utc));
        if (to.HasValue)
        {
            var upperBound = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            if (upperBound.TimeOfDay == TimeSpan.Zero) upperBound = upperBound.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.RequestedAtUtc <= upperBound);
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.RequestedAtUtc).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id, provider = x.PaymentProviderConfiguration!.DisplayName, x.Environment, x.Operation, x.HttpMethod,
                x.Endpoint, x.TransactionReference, x.RequestedAtUtc, x.HttpStatusCode, x.DurationMilliseconds,
                x.AttemptNumber, x.ErrorCategory, x.CorrelationId, x.ConfigurationVersion,
                requestHeaders = x.SanitizedRequestHeaders, requestPayload = x.SanitizedRequestPayload,
                responseHeaders = x.SanitizedResponseHeaders, responsePayload = x.SanitizedResponsePayload
            }).ToListAsync(cancellationToken);
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("transactions/{id:guid}")]
    public async Task<IActionResult> Inspect(Guid id, CancellationToken cancellationToken)
    {
        var payment = await db.DigitalCollections.AsNoTracking()
            .Include(x => x.PaymentProviderConfiguration).Include(x => x.Lines).ThenInclude(x => x.ContributionType)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payment is null) return NotFound();
        var events = await db.DigitalCollectionEvents.AsNoTracking().Where(x => x.DigitalCollectionId == id)
            .OrderBy(x => x.OccurredAtUtc).Select(x => new { x.EventType, x.PreviousStatus, x.NewStatus, x.Source, x.CorrelationId, x.OccurredAtUtc }).ToListAsync(cancellationToken);
        var exchanges = await db.PaymentApiExchanges.AsNoTracking().Where(x => x.DigitalCollectionId == id)
            .OrderBy(x => x.RequestedAtUtc).Select(x => new { x.Id, x.Operation, x.HttpMethod, x.Endpoint, x.HttpStatusCode, x.DurationMilliseconds, x.ErrorCategory, x.RequestedAtUtc }).ToListAsync(cancellationToken);
        return Ok(new
        {
            payment.Id, payment.PaymentReference, payment.ProviderTransactionId, payment.GrossAmount, payment.CurrencyCode,
            provider = payment.PaymentProviderConfiguration!.DisplayName, payment.ProviderEnvironmentSnapshot,
            payment.Status, payment.SettlementStatus, payment.ReconciliationStatus, payment.ReconciliationAttemptCount,
            payment.InitiatedAtUtc, payment.ProviderCompletedAtUtc,
            ledgerPosted = payment.Lines.All(x => x.ContributionId != null),
            lines = payment.Lines.Select(x => new { category = x.ContributionType!.Name, x.Amount, x.ContributionId }),
            events, exchanges
        });
    }

    private Guid Actor() => currentUser.GetUserId() ?? throw new UnauthorizedAccessException();
    private Task AuditAsync(string action, string message, CancellationToken cancellationToken) => logs.WriteAsync(new SystemLog
    {
        Level = "Information", Category = "Audit", Process = "MtnDeveloperMode", Action = action,
        UserId = Actor(), CorrelationId = HttpContext.TraceIdentifier, Message = message
    }, cancellationToken);
    public sealed record RestoreRequest(string Reason);
}
