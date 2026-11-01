using eMilimo.API.Authorization;
using eMilimo.API.Services;
using eMilimo.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/accounting-integrations"), Authorize]
public sealed class AccountingIntegrationsController(
    IAccountingIntegrationService accounting,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("profiles"), RequirePermission("accounting.integration.view")]
    public async Task<IActionResult> Profiles(CancellationToken cancellationToken) =>
        Ok(await accounting.GetProfilesAsync(cancellationToken));

    [HttpPost("profiles"), RequirePermission("accounting.integration.manage")]
    public async Task<IActionResult> CreateProfile(AccountingProfileInput request, CancellationToken cancellationToken)
    {
        var result = await accounting.SaveProfileAsync(null, request, Actor(), cancellationToken);
        return CreatedAtAction(nameof(Profiles), new { id = result.Id }, result);
    }

    [HttpPut("profiles/{id:guid}"), RequirePermission("accounting.integration.manage")]
    public async Task<IActionResult> UpdateProfile(Guid id, AccountingProfileInput request, CancellationToken cancellationToken) =>
        Ok(await accounting.SaveProfileAsync(id, request, Actor(), cancellationToken));

    [HttpGet("profiles/{profileId:guid}/mappings"), RequirePermission("accounting.integration.view")]
    public async Task<IActionResult> Mappings(Guid profileId, CancellationToken cancellationToken) =>
        Ok(await accounting.GetMappingsAsync(profileId, cancellationToken));

    [HttpPost("mappings"), RequirePermission("accounting.integration.manage")]
    public async Task<IActionResult> CreateMapping(AccountingMappingInput request, CancellationToken cancellationToken) =>
        Ok(await accounting.SaveMappingAsync(null, request, Actor(), cancellationToken));

    [HttpPut("mappings/{id:guid}"), RequirePermission("accounting.integration.manage")]
    public async Task<IActionResult> UpdateMapping(Guid id, AccountingMappingInput request, CancellationToken cancellationToken) =>
        Ok(await accounting.SaveMappingAsync(id, request, Actor(), cancellationToken));

    [HttpGet("mapping-catalog"), RequirePermission("accounting.integration.view")]
    public async Task<IActionResult> MappingCatalog(CancellationToken cancellationToken) =>
        Ok(await accounting.GetMappingCatalogAsync(cancellationToken));

    [HttpPost("batches/preflight"), RequirePermission("accounting.batch.prepare")]
    public async Task<IActionResult> Preflight(PrepareAccountingBatchRequest request, CancellationToken cancellationToken) =>
        Ok(await accounting.PreflightAsync(request, cancellationToken));

    [HttpPost("batches/prepare"), RequirePermission("accounting.batch.prepare")]
    public async Task<IActionResult> Prepare(PrepareAccountingBatchRequest request, CancellationToken cancellationToken) =>
        Ok(await accounting.PrepareBatchAsync(request, Actor(), cancellationToken));

    [HttpGet("batches"), RequirePermission("accounting.integration.view")]
    public async Task<IActionResult> Batches([FromQuery] Guid? profileId, [FromQuery] string? status, CancellationToken cancellationToken) =>
        Ok(await accounting.GetBatchesAsync(profileId, status, cancellationToken));

    [HttpGet("batches/{id:guid}"), RequirePermission("accounting.integration.view")]
    public async Task<IActionResult> Batch(Guid id, CancellationToken cancellationToken)
    {
        var result = await accounting.GetBatchAsync(id, cancellationToken);
        return result is null ? NotFound(new { message = "Accounting batch was not found." }) : Ok(result);
    }

    [HttpPost("batches/{id:guid}/approve"), RequirePermission("accounting.batch.approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken) =>
        Ok(await accounting.ApproveBatchAsync(id, Actor(), cancellationToken));

    [HttpPost("batches/{id:guid}/cancel"), RequirePermission("accounting.batch.prepare")]
    public async Task<IActionResult> Cancel(Guid id, CancelAccountingBatchRequest request, CancellationToken cancellationToken) =>
        Ok(await accounting.CancelBatchAsync(id, request.Reason, Actor(), cancellationToken));

    [HttpPost("batches/{id:guid}/export"), RequirePermission("accounting.batch.export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken cancellationToken)
    {
        var file = await accounting.ExportBatchAsync(id, Actor(), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private Guid Actor() => currentUser.GetUserId() ?? throw new UnauthorizedAccessException();
}
