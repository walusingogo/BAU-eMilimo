using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eMilimo.API.Authorization;
using eMilimo.Core.DTOs;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using eMilimo.API.Services;
using System.Text.Json;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/frb-settings"), Authorize]
public sealed class FrbSettingsController(AppDbContext db, ICurrentUserService current, ISystemLogWriter logs) : ControllerBase
{
    private Guid UserId() => current.GetUserId() ?? throw new UnauthorizedAccessException();

    [HttpGet("assessment-rules"), RequirePermission("frb.view")]
    public async Task<IActionResult> Rules(CancellationToken ct) => Ok(await db.FrbAssessmentRules.AsNoTracking()
        .Include(x => x.ContributionTypes).ThenInclude(x => x.ContributionType).Include(x => x.AllocationLines)
        .OrderBy(x => x.Name).ThenByDescending(x => x.EffectiveFrom)
        .Select(x => new { x.Id, x.Name, x.Description, x.Classification, x.CalculationMethod,
            x.EffectiveFrom, x.EffectiveTo, x.IsActive, x.Notes,
            ContributionTypes = x.ContributionTypes.Select(t => new { t.ContributionTypeId, t.ContributionType!.Name }),
            AllocationLines = x.AllocationLines.OrderBy(l => l.DisplayOrder).Select(l => new
                { l.Id, l.Beneficiary, l.Rate, l.FixedAmount, l.IsLocalRetained, l.DisplayOrder, l.Notes }) }).ToListAsync(ct));

    [HttpPost("assessment-rules"), RequirePermission("frb.rules.manage")]
    public async Task<IActionResult> CreateRule(SaveFrbAssessmentRuleRequest request, CancellationToken ct)
    {
        var error = await ValidateRule(request, null, ct); if (error != null) return BadRequest(new { message = error });
        var actor = UserId(); var rule = MapRule(request, actor); db.FrbAssessmentRules.Add(rule); await db.SaveChangesAsync(ct);
        await AuditAsync("AssessmentSchemeCreated", rule.Id, new { After = AuditView(rule) }, actor, ct);
        foreach (var line in rule.AllocationLines)
            await AuditAsync("AssessmentAllocationLineAdded", rule.Id, new { After = LineAuditView(line) }, actor, ct);
        return Ok(new { rule.Id });
    }

    [HttpPut("assessment-rules/{id:guid}"), RequirePermission("frb.rules.manage")]
    public async Task<IActionResult> UpdateRule(Guid id, SaveFrbAssessmentRuleRequest request, CancellationToken ct)
    {
        var existing = await db.FrbAssessmentRules.Include(x => x.ContributionTypes).Include(x => x.AllocationLines).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing == null) return NotFound(new { message = "Assessment rule not found." });
        if (await db.FrbAssessmentSnapshots.AnyAsync(x => x.SourceRuleId == id, ct))
            return BadRequest(new { message = "This rule has been used by an FRB. Create a new effective-dated version instead of changing historical financial logic." });
        var error = await ValidateRule(request, id, ct); if (error != null) return BadRequest(new { message = error });
        var before = AuditView(existing); var beforeTypes = existing.ContributionTypes.Select(x => x.ContributionTypeId).OrderBy(x => x).ToArray();
        var beforeLines = existing.AllocationLines.OrderBy(x => x.DisplayOrder).Select(LineAuditView).ToList(); var actor = UserId();
        existing.Name = request.Name.Trim(); existing.Description = request.Description?.Trim(); existing.Classification = request.Classification.ToUpperInvariant();
        existing.CalculationMethod = request.CalculationMethod.ToUpperInvariant(); existing.EffectiveFrom = request.EffectiveFrom; existing.EffectiveTo = request.EffectiveTo;
        existing.IsActive = request.IsActive; existing.Notes = request.Notes?.Trim(); existing.UpdatedById = UserId(); existing.UpdatedDate = DateTime.UtcNow; existing.Version++;
        SyncContributionTypes(existing, request.ContributionTypeIds);
        SyncAllocationLines(existing, NormalizeLines(request), actor);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This assessment rule changed while you were editing it. Reload the rule and try again." });
        }
        await AuditAsync("AssessmentSchemeUpdated", existing.Id, new { Before = before, After = AuditView(existing) }, actor, ct);
        var afterTypes = existing.ContributionTypes.Select(x => x.ContributionTypeId).OrderBy(x => x).ToArray();
        if (!beforeTypes.SequenceEqual(afterTypes)) await AuditAsync("AssessmentContributionEligibilityChanged", existing.Id, new { Before = beforeTypes, After = afterTypes }, actor, ct);
        await AuditLineChanges(existing.Id, beforeLines, existing.AllocationLines.OrderBy(x => x.DisplayOrder).Select(LineAuditView).ToList(), actor, ct);
        return Ok(new { existing.Id });
    }

    [HttpGet("reference-numbering"), RequirePermission("settings.manage")]
    public async Task<IActionResult> ReferenceNumbering(CancellationToken ct) => Ok(await db.ReferenceNumberConfigurations.AsNoTracking()
        .OrderBy(x => x.ReferenceType).ThenByDescending(x => x.EffectiveFrom).ToListAsync(ct));

    [HttpPost("reference-numbering"), RequirePermission("settings.manage")]
    public async Task<IActionResult> AddReferenceConfiguration(SaveReferenceNumberConfigurationRequest request, CancellationToken ct)
    {
        var type = request.ReferenceType.Trim().ToUpperInvariant(); var prefix = request.Prefix.Trim().ToUpperInvariant();
        if (type is not ("CONTRIBUTION" or "FRB" or "REQUISITION" or "ASSET" or "SERVICE_COLLECTION")) return BadRequest(new { message = "Unknown reference type." });
        if (prefix.Length is < 1 or > 10 || prefix.Any(x => !char.IsAsciiLetterOrDigit(x))) return BadRequest(new { message = "Prefix must contain 1-10 letters or digits." });
        if (await db.ReferenceNumberConfigurations.AnyAsync(x => x.ReferenceType == type && x.EffectiveFrom == request.EffectiveFrom, ct))
            return Conflict(new { message = "A numbering configuration already starts on that date." });
        if (await db.ReferenceNumberConfigurations.AnyAsync(x => x.ReferenceType != type && x.Prefix == prefix, ct))
            return Conflict(new { message = "That prefix belongs to another reference type and cannot be reused." });
        var item = new ReferenceNumberConfiguration { Id = Guid.NewGuid(), ReferenceType = type, Prefix = prefix,
            Description = request.Description.Trim(), EffectiveFrom = request.EffectiveFrom, IsActive = request.IsActive,
            CreatedById = UserId(), UpdatedById = UserId() };
        db.ReferenceNumberConfigurations.Add(item); await db.SaveChangesAsync(ct);
        await AuditAsync("ReferenceNumberConfigurationCreated", item.Id, new { item.ReferenceType, item.Prefix, item.EffectiveFrom }, UserId(), ct); return Ok(item);
    }

    private async Task<string?> ValidateRule(SaveFrbAssessmentRuleRequest x, Guid? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(x.Name)) return "Assessment scheme name is required.";
        if (x.ContributionTypeIds == null || x.ContributionTypeIds.Count == 0) return "Select at least one applicable Contribution Type.";
        if (x.EffectiveTo.HasValue && x.EffectiveTo < x.EffectiveFrom) return "Effective To must be on or after Effective From.";
        var method = x.CalculationMethod.Trim().ToUpperInvariant(); var classification = x.Classification.Trim().ToUpperInvariant();
        var lines = NormalizeLines(x);
        var financialError = FrbFinancialCalculator.ValidateScheme(classification, method, x.IsActive,
            lines.Select(line => (line.Beneficiary, line.Rate, line.FixedAmount, line.IsLocalRetained)).ToList());
        if (financialError != null) return financialError;
        if (await db.ContributionTypes.CountAsync(t => x.ContributionTypeIds.Contains(t.Id), ct) != x.ContributionTypeIds.Distinct().Count()) return "One or more Contribution Types do not exist.";
        var normalizedName = x.Name.Trim().ToLower();
        var overlaps = await db.FrbAssessmentRules.AnyAsync(r => r.Id != id && r.IsActive && x.IsActive &&
            r.Name.ToLower() == normalizedName && r.EffectiveFrom <= (x.EffectiveTo ?? DateOnly.MaxValue) &&
            (r.EffectiveTo ?? DateOnly.MaxValue) >= x.EffectiveFrom, ct);
        if (overlaps) return "This scheme overlaps an active effective-dated version with the same name.";
        return null;
    }

    private static FrbAssessmentRule MapRule(SaveFrbAssessmentRuleRequest x, Guid actor) => new()
    {
        Id = Guid.NewGuid(), Name = x.Name.Trim(), Description = x.Description?.Trim(), Classification = x.Classification.ToUpperInvariant(),
        CalculationMethod = x.CalculationMethod.ToUpperInvariant(),
        EffectiveFrom = x.EffectiveFrom, EffectiveTo = x.EffectiveTo, IsActive = x.IsActive, Notes = x.Notes?.Trim(), CreatedById = actor, UpdatedById = actor,
        ContributionTypes = x.ContributionTypeIds.Distinct().Select(id => new FrbAssessmentRuleContributionType { ContributionTypeId = id }).ToList(),
        AllocationLines = NormalizeLines(x).Select((line, index) => MapLine(line, actor, index)).ToList()
    };

    private static IReadOnlyCollection<SaveFrbAssessmentAllocationLineRequest> NormalizeLines(SaveFrbAssessmentRuleRequest x)
    {
        return x.AllocationLines ?? [];
    }

    private void SyncContributionTypes(FrbAssessmentRule rule, IReadOnlyCollection<Guid> requestedTypeIds)
    {
        var remainingTypeIds = requestedTypeIds.Distinct().ToHashSet();
        foreach (var existingType in rule.ContributionTypes.ToList())
        {
            if (remainingTypeIds.Remove(existingType.ContributionTypeId)) continue;
            rule.ContributionTypes.Remove(existingType);
            db.FrbAssessmentRuleContributionTypes.Remove(existingType);
        }

        foreach (var typeId in remainingTypeIds)
        {
            var contributionType = new FrbAssessmentRuleContributionType
            {
                FrbAssessmentRuleId = rule.Id,
                ContributionTypeId = typeId
            };
            db.FrbAssessmentRuleContributionTypes.Add(contributionType);
            rule.ContributionTypes.Add(contributionType);
        }
    }

    private void SyncAllocationLines(
        FrbAssessmentRule rule,
        IReadOnlyCollection<SaveFrbAssessmentAllocationLineRequest> requestedLines,
        Guid actor)
    {
        var lines = requestedLines.ToList();
        var existingLines = rule.AllocationLines.OrderBy(line => line.DisplayOrder).ToList();
        var sharedCount = Math.Min(existingLines.Count, lines.Count);

        for (var index = 0; index < sharedCount; index++)
        {
            var existingLine = existingLines[index];
            var requestedLine = lines[index];
            var beneficiary = requestedLine.Beneficiary.Trim();
            var notes = requestedLine.Notes?.Trim();
            var displayOrder = requestedLine.DisplayOrder >= 0 ? requestedLine.DisplayOrder : index;

            if (existingLine.Beneficiary == beneficiary && existingLine.Rate == requestedLine.Rate &&
                existingLine.FixedAmount == requestedLine.FixedAmount && existingLine.IsLocalRetained == requestedLine.IsLocalRetained &&
                existingLine.DisplayOrder == displayOrder && existingLine.Notes == notes)
                continue;

            existingLine.Beneficiary = beneficiary;
            existingLine.Rate = requestedLine.Rate;
            existingLine.FixedAmount = requestedLine.FixedAmount;
            existingLine.IsLocalRetained = requestedLine.IsLocalRetained;
            existingLine.DisplayOrder = displayOrder;
            existingLine.Notes = notes;
            existingLine.UpdatedById = actor;
            existingLine.UpdatedDate = DateTime.UtcNow;
            existingLine.Version++;
        }

        foreach (var removedLine in existingLines.Skip(lines.Count))
        {
            rule.AllocationLines.Remove(removedLine);
            db.FrbAssessmentAllocationLines.Remove(removedLine);
        }

        for (var index = existingLines.Count; index < lines.Count; index++)
        {
            var allocationLine = MapLine(lines[index], actor, index, rule.Id);
            db.FrbAssessmentAllocationLines.Add(allocationLine);
            rule.AllocationLines.Add(allocationLine);
        }
    }

    private static FrbAssessmentAllocationLine MapLine(SaveFrbAssessmentAllocationLineRequest line, Guid actor, int index, Guid? ruleId = null) => new()
    {
        Id = Guid.NewGuid(), FrbAssessmentRuleId = ruleId ?? Guid.Empty, Beneficiary = line.Beneficiary.Trim(), Rate = line.Rate,
        FixedAmount = line.FixedAmount, IsLocalRetained = line.IsLocalRetained, DisplayOrder = line.DisplayOrder >= 0 ? line.DisplayOrder : index,
        Notes = line.Notes?.Trim(), CreatedById = actor, UpdatedById = actor
    };

    private static object AuditView(FrbAssessmentRule rule) => new
    {
        rule.Name, rule.Description, rule.Classification, rule.CalculationMethod, rule.EffectiveFrom, rule.EffectiveTo, rule.IsActive, rule.Notes,
        ContributionTypeIds = rule.ContributionTypes.Select(x => x.ContributionTypeId).OrderBy(x => x),
        AllocationLines = rule.AllocationLines.OrderBy(x => x.DisplayOrder).Select(x => new { x.Beneficiary, x.Rate, x.FixedAmount, x.IsLocalRetained, x.DisplayOrder, x.Notes })
    };

    private static AllocationLineAudit LineAuditView(FrbAssessmentAllocationLine line) =>
        new(line.Beneficiary, line.Rate, line.FixedAmount, line.IsLocalRetained, line.DisplayOrder, line.Notes);

    private async Task AuditLineChanges(Guid ruleId, IReadOnlyList<AllocationLineAudit> before, IReadOnlyList<AllocationLineAudit> after, Guid actor, CancellationToken ct)
    {
        var length = Math.Max(before.Count, after.Count);
        for (var index = 0; index < length; index++)
        {
            var oldLine = index < before.Count ? before[index] : null; var newLine = index < after.Count ? after[index] : null;
            if (oldLine == newLine) continue;
            var action = oldLine == null ? "AssessmentAllocationLineAdded" : newLine == null ? "AssessmentAllocationLineRemoved" : "AssessmentAllocationLineChanged";
            await AuditAsync(action, ruleId, new { Before = oldLine, After = newLine }, actor, ct);
        }
    }

    private sealed record AllocationLineAudit(string Beneficiary, decimal? Rate, decimal? FixedAmount, bool IsLocalRetained, int DisplayOrder, string? Notes);

    private Task AuditAsync(string action, Guid entityId, object details, Guid actor, CancellationToken ct) => logs.WriteAsync(new SystemLog
    {
        Level = "Information", Category = "Audit", Process = "FRB Settings", Action = action, UserId = actor,
        CorrelationId = HttpContext.TraceIdentifier, Message = $"{action} for {entityId}.", Details = JsonSerializer.Serialize(details)
    }, ct);
}
