using System.Text.Json;
using eMilimo.API.Authorization;
using eMilimo.API.Services;
using eMilimo.Core.DTOs;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/members"), Authorize]
public sealed class MembersController(AppDbContext db, ICurrentUserService current, ISystemLogWriter logs) : ControllerBase
{
    private Guid UserId() => current.GetUserId() ?? throw new UnauthorizedAccessException();

    [HttpGet("metadata"), RequirePermission("members.view")]
    public async Task<IActionResult> Metadata(CancellationToken ct) => Ok(new
    {
        Sections = await db.Sections.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SectionName)
            .Select(x => new { x.Id, Name = x.SectionName }).ToListAsync(ct),
        Groups = await db.Groups.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.GroupName)
            .Select(x => new { x.Id, Name = x.GroupName }).ToListAsync(ct)
    });

    [HttpGet, RequirePermission("members.view")]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] bool includeArchived = false, [FromQuery] int offset = 0, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var query = db.Members.AsNoTracking().Include(x => x.Section).Include(x => x.Group).AsQueryable();
        if (!includeArchived) query = query.Where(x => !x.IsArchived);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpper());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToUpperInvariant();
            query = query.Where(x => x.MembershipNumber.ToUpper().Contains(value) || x.NormalizedSearchName.Contains(value) ||
                (x.NormalizedPhoneNumber != null && x.NormalizedPhoneNumber.Contains(value)) ||
                (x.Email != null && x.Email.ToUpper().Contains(value)));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 200))
            .Select(x => Summary(x)).ToListAsync(ct);
        return Ok(new { total, items });
    }

    [HttpGet("{id:guid}"), RequirePermission("members.view")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var member = await db.Members.AsNoTracking().Include(x => x.Section).Include(x => x.Group)
            .Include(x => x.ExternalIdentities).Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return member == null ? NotFound(new { message = "Member not found." }) : Ok(Detail(member));
    }

    [HttpPost, RequirePermission("members.manage")]
    public async Task<IActionResult> Create(SaveMemberRequest request, CancellationToken ct)
    {
        var error = await Validate(request, null, ct); if (error != null) return BadRequest(new { message = error });
        var actor = UserId(); var now = DateTime.UtcNow;
        var member = new Member { Id = Guid.NewGuid(), CreatedById = actor, UpdatedById = actor, CreatedDate = now, UpdatedDate = now };
        Apply(member, request);
        db.Members.Add(member);
        db.MembershipStatusHistory.Add(new MembershipStatusHistory { Id = Guid.NewGuid(), MemberId = member.Id,
            PreviousStatus = string.Empty, NewStatus = member.Status, EffectiveAtUtc = now, Reason = "Member created.",
            ChangedById = actor, CreatedById = actor, UpdatedById = actor });
        AddAudit(member.Id, "CREATED", new { After = AuditView(member) }, actor);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = member.Id }, Detail(member));
    }

    [HttpPut("{id:guid}"), RequirePermission("members.manage")]
    public async Task<IActionResult> Update(Guid id, SaveMemberRequest request, CancellationToken ct)
    {
        var member = await db.Members.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (member == null) return NotFound(new { message = "Member not found." });
        if (!request.Version.HasValue || request.Version != member.Version) return Conflict(new { message = "This member changed while you were editing. Reload and try again." });
        var error = await Validate(request, id, ct); if (error != null) return BadRequest(new { message = error });
        var before = AuditView(member); var oldStatus = member.Status; var actor = UserId();
        Apply(member, request); member.UpdatedById = actor; member.UpdatedDate = DateTime.UtcNow; member.Version++;
        if (oldStatus != member.Status) AddStatus(member, oldStatus, member.Status, "Status changed during member update.", actor);
        AddAudit(id, "UPDATED", new { Before = before, After = AuditView(member) }, actor);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "This member changed while you were editing. Reload and try again." }); }
        return Ok(Detail(member));
    }

    [HttpPut("{id:guid}/status"), RequirePermission("members.manage")]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeMembershipStatusRequest request, CancellationToken ct)
    {
        var member = await db.Members.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (member == null) return NotFound(new { message = "Member not found." });
        var status = request.Status.Trim().ToUpperInvariant();
        if (!MembershipStatuses.All.Contains(status)) return BadRequest(new { message = "Unknown membership status." });
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { message = "A status-change reason is required." });
        if (member.Version != request.Version) return Conflict(new { message = "This member changed while you were editing. Reload and try again." });
        var actor = UserId(); var previous = member.Status;
        member.Status = status; member.IsActive = status == MembershipStatuses.Active; member.UpdatedById = actor; member.UpdatedDate = DateTime.UtcNow; member.Version++;
        AddStatus(member, previous, status, request.Reason.Trim(), actor);
        AddAudit(id, "STATUS_CHANGED", new { Before = previous, After = status, request.Reason }, actor);
        await db.SaveChangesAsync(ct); return Ok(Detail(member));
    }

    [HttpPut("{id:guid}/archive"), RequirePermission("members.manage")]
    public async Task<IActionResult> Archive(Guid id, ArchiveMemberRequest request, CancellationToken ct)
    {
        var member = await db.Members.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (member == null) return NotFound(new { message = "Member not found." });
        if (member.Version != request.Version) return Conflict(new { message = "This member changed while you were editing. Reload and try again." });
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { message = "An archive reason is required." });
        var actor = UserId(); var previous = member.Status;
        member.IsArchived = true; member.IsActive = false; member.Status = MembershipStatuses.Archived;
        member.ArchivedAtUtc = DateTime.UtcNow; member.ArchivedById = actor; member.ArchiveReason = request.Reason.Trim();
        member.UpdatedById = actor; member.UpdatedDate = DateTime.UtcNow; member.Version++;
        AddStatus(member, previous, member.Status, request.Reason.Trim(), actor); AddAudit(id, "ARCHIVED", new { request.Reason }, actor);
        await db.SaveChangesAsync(ct); return Ok(Detail(member));
    }

    [HttpPut("{id:guid}/restore"), RequirePermission("members.manage")]
    public async Task<IActionResult> Restore(Guid id, ChangeMembershipStatusRequest request, CancellationToken ct)
    {
        var member = await db.Members.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (member == null) return NotFound(new { message = "Member not found." });
        if (member.Version != request.Version) return Conflict(new { message = "This member changed while you were editing. Reload and try again." });
        var status = request.Status.Trim().ToUpperInvariant();
        if (status == MembershipStatuses.Archived || !MembershipStatuses.All.Contains(status)) return BadRequest(new { message = "Select a non-archived membership status." });
        var actor = UserId(); member.IsArchived = false; member.IsActive = status == MembershipStatuses.Active; member.Status = status;
        member.ArchivedAtUtc = null; member.ArchivedById = null; member.ArchiveReason = null; member.UpdatedById = actor; member.UpdatedDate = DateTime.UtcNow; member.Version++;
        AddStatus(member, MembershipStatuses.Archived, status, request.Reason, actor); AddAudit(id, "RESTORED", new { status, request.Reason }, actor);
        await db.SaveChangesAsync(ct); return Ok(Detail(member));
    }

    [HttpPost("{sourceId:guid}/merge/{targetId:guid}"), RequirePermission("members.manage")]
    public async Task<IActionResult> Merge(Guid sourceId, Guid targetId, MergeMemberRequest request, CancellationToken ct)
    {
        if (sourceId == targetId) return BadRequest(new { message = "Source and target member must be different." });
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { message = "A merge reason is required." });
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var source = await db.Members.Include(x => x.ExternalIdentities).FirstOrDefaultAsync(x => x.Id == sourceId, ct);
        var target = await db.Members.FirstOrDefaultAsync(x => x.Id == targetId, ct);
        if (source == null || target == null) return NotFound(new { message = "Source or target member not found." });
        if (source.Version != request.SourceVersion || target.Version != request.TargetVersion) return Conflict(new { message = "One of the members changed. Reload and try again." });
        if (source.ExternalIdentities.Any(x => db.MemberExternalIdentities.Any(y => y.MemberId == targetId && y.ProviderCode == x.ProviderCode && y.ExternalMemberId != x.ExternalMemberId)))
            return Conflict(new { message = "The members have conflicting identities from the same external provider. Resolve that conflict before merging." });
        var actor = UserId();
        foreach (var contribution in await db.Contributions.Where(x => x.MemberId == sourceId).ToListAsync(ct)) contribution.MemberId = targetId;
        foreach (var entry in await db.ServiceCollectionEntries.Where(x => x.MemberId == sourceId).ToListAsync(ct)) entry.MemberId = targetId;
        foreach (var identity in source.ExternalIdentities) identity.MemberId = targetId;
        source.IsArchived = true; source.IsActive = false; source.Status = MembershipStatuses.Archived; source.ArchiveReason = $"Merged into {target.MembershipNumber}: {request.Reason.Trim()}";
        source.ArchivedAtUtc = DateTime.UtcNow; source.ArchivedById = actor; source.UpdatedById = actor; source.UpdatedDate = DateTime.UtcNow; source.Version++;
        target.UpdatedById = actor; target.UpdatedDate = DateTime.UtcNow; target.Version++;
        AddAudit(sourceId, "MERGED_SOURCE", new { TargetMemberId = targetId, request.Reason }, actor);
        AddAudit(targetId, "MERGED_TARGET", new { SourceMemberId = sourceId, request.Reason }, actor);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(new { targetId });
    }

    [HttpPost("import"), RequirePermission("members.manage")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Import(ImportMembersRequest request, CancellationToken ct)
    {
        var rows = request.Rows ?? [];
        if (rows.Count == 0) return BadRequest(new { message = "The import file does not contain any member records." });
        if (rows.Count > 5000) return BadRequest(new { message = "A single import is limited to 5,000 member records." });
        var mode = request.Mode.Trim().ToUpperInvariant();
        if (mode is not ("CREATE_ONLY" or "UPSERT"))
            return BadRequest(new { message = "Import mode must be CREATE_ONLY or UPSERT." });

        var sections = await db.Sections.AsNoTracking().Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.SectionName.Trim().ToUpper(), x => x.Id, ct);
        var groups = await db.Groups.AsNoTracking().Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.GroupName.Trim().ToUpper(), x => x.Id, ct);
        var numbers = rows.Select(x => x.MembershipNumber.Trim().ToUpperInvariant()).Where(x => x.Length > 0).Distinct().ToArray();
        var existing = await db.Members.Where(x => numbers.Contains(x.MembershipNumber.ToUpper()))
            .ToDictionaryAsync(x => x.MembershipNumber.ToUpper(), ct);
        var duplicateNumbers = rows.Select((row, index) => new { Number = row.MembershipNumber.Trim().ToUpperInvariant(), Row = index + 2 })
            .Where(x => x.Number.Length > 0).GroupBy(x => x.Number).Where(x => x.Count() > 1).Select(x => x.Key).ToHashSet();
        var errors = new List<object>();
        var prepared = new List<(int Row, ImportMemberRow Source, SaveMemberRequest Request, Member? Existing)>();

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];
            var number = row.MembershipNumber.Trim().ToUpperInvariant();
            if (number.Length == 0 || string.IsNullOrWhiteSpace(row.FirstName) || string.IsNullOrWhiteSpace(row.LastName))
            { errors.Add(new { row = rowNumber, message = "Membership number, first name and surname are required." }); continue; }
            if (duplicateNumbers.Contains(number))
            { errors.Add(new { row = rowNumber, message = $"Membership number {number} occurs more than once in the file." }); continue; }
            var status = row.Status.Trim().ToUpperInvariant();
            if (!MembershipStatuses.All.Contains(status))
            { errors.Add(new { row = rowNumber, message = $"Unknown membership status '{row.Status}'." }); continue; }
            if (!string.IsNullOrWhiteSpace(row.Email))
            {
                try { _ = new System.Net.Mail.MailAddress(row.Email.Trim()); }
                catch { errors.Add(new { row = rowNumber, message = $"'{row.Email}' is not a valid email address." }); continue; }
            }
            Guid? sectionId = null; Guid? groupId = null;
            if (!string.IsNullOrWhiteSpace(row.Section))
            {
                if (!sections.TryGetValue(row.Section.Trim().ToUpperInvariant(), out var resolvedSection))
                { errors.Add(new { row = rowNumber, message = $"Section '{row.Section}' is not active or does not exist." }); continue; }
                sectionId = resolvedSection;
            }
            if (!string.IsNullOrWhiteSpace(row.Group))
            {
                if (!groups.TryGetValue(row.Group.Trim().ToUpperInvariant(), out var resolvedGroup))
                { errors.Add(new { row = rowNumber, message = $"Group '{row.Group}' is not active or does not exist." }); continue; }
                groupId = resolvedGroup;
            }
            existing.TryGetValue(number, out var currentMember);
            prepared.Add((rowNumber, row, new SaveMemberRequest
            {
                MembershipNumber = number, Title = row.Title, FirstName = row.FirstName, MiddleNames = row.MiddleNames,
                LastName = row.LastName, PreferredName = row.PreferredName, PhoneNumber = row.PhoneNumber, Email = row.Email,
                MembershipType = string.IsNullOrWhiteSpace(row.MembershipType) ? "MEMBER" : row.MembershipType,
                Status = status, SectionId = sectionId, GroupId = groupId, JoinedOn = row.JoinedOn,
                DateOfBirth = row.DateOfBirth, Sex = row.Sex, Address = row.Address
            }, currentMember));
        }

        var creates = prepared.Count(x => x.Existing == null);
        var updates = mode == "UPSERT" ? prepared.Count(x => x.Existing != null) : 0;
        var skipped = mode == "CREATE_ONLY" ? prepared.Count(x => x.Existing != null) : 0;
        if (errors.Count > 0)
            return UnprocessableEntity(new { message = "Correct the import errors before importing any records.", total = rows.Count, creates, updates, skipped, errors });
        if (request.DryRun)
            return Ok(new { dryRun = true, total = rows.Count, creates, updates, skipped, errors = Array.Empty<object>() });

        var actor = UserId(); var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var item in prepared)
        {
            if (item.Existing == null)
            {
                var member = new Member { Id = Guid.NewGuid(), CreatedById = actor, UpdatedById = actor, CreatedDate = now, UpdatedDate = now, RecordSource = "IMPORT", SynchronizationStatus = "IMPORTED" };
                Apply(member, item.Request); db.Members.Add(member);
                AddStatus(member, string.Empty, member.Status, "Member imported from file.", actor);
                AddAudit(member.Id, "IMPORTED", new { Row = item.Row, After = AuditView(member) }, actor);
            }
            else if (mode == "UPSERT")
            {
                var member = item.Existing; var before = AuditView(member); var previousStatus = member.Status;
                Apply(member, item.Request); member.RecordSource = "IMPORT"; member.SynchronizationStatus = "IMPORTED";
                member.UpdatedById = actor; member.UpdatedDate = now; member.Version++;
                if (previousStatus != member.Status) AddStatus(member, previousStatus, member.Status, "Status changed by membership import.", actor);
                AddAudit(member.Id, "IMPORT_UPDATED", new { Row = item.Row, Before = before, After = AuditView(member) }, actor);
            }
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        await logs.WriteAsync(new SystemLog { Category = "Audit", Process = "Membership", Action = "MembersImported", UserId = actor,
            CorrelationId = HttpContext.TraceIdentifier, Message = $"Membership import completed: {creates} created, {updates} updated, {skipped} skipped.",
            Details = JsonSerializer.Serialize(new { Total = rows.Count, Creates = creates, Updates = updates, Skipped = skipped, Mode = mode }) }, ct);
        return Ok(new { dryRun = false, total = rows.Count, creates, updates, skipped, errors = Array.Empty<object>() });
    }

    [HttpGet("{id:guid}/financial-activity"), RequirePermission("members.financial-activity")]
    public async Task<IActionResult> FinancialActivity(Guid id, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from.Date > to.Date || (to.Date - from.Date).TotalDays > 3660) return BadRequest(new { message = "Select a valid reporting period of no more than ten years." });
        var member = await db.Members.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (member == null) return NotFound(new { message = "Member not found." });
        // Query-string dates are parsed with DateTimeKind.Unspecified. PostgreSQL timestamptz
        // parameters require UTC, so define the reporting boundaries explicitly as UTC.
        var start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc);
        var direct = await db.Contributions.AsNoTracking().Include(x => x.ContributionType)
            .Where(x => x.MemberId == id && x.ContributionDate >= start && x.ContributionDate < end)
            .Select(x => new MemberFinancialActivityItem(x.Id, x.ContributionNumber, x.ContributionDate, x.ContributionType!.Name,
                x.Amount, x.CurrencyCode, x.OriginalCurrencyAmount, x.ExchangeRateSnapshot, x.PaymentMethod,
                x.IsArchived ? "ARCHIVED" : x.Status, x.Status, x.Description, x.ReferenceNumber, x.FinalizedFinancialRecordBookId,
                "CONTRIBUTION", x.ReversesContributionId != null, $"/contributions/{x.Id}"))
            .ToListAsync(ct);
        var items = direct.OrderByDescending(x => x.ContributionDate).ToList();
        var official = items.Where(x => x.Status != "ARCHIVED" &&
            (x.FinancialStatus is "CONFIRMED" or "ALLOCATED" or ServiceCollectionStatuses.Approved) && !x.IsReversal).Sum(x => x.Amount);
        var reversed = items.Where(x => x.IsReversal).Sum(x => x.Amount);
        await logs.WriteAsync(new SystemLog { Category = "Audit", Process = "Membership", Action = "FinancialActivityViewed",
            UserId = UserId(), CorrelationId = HttpContext.TraceIdentifier, Message = $"Member financial activity viewed for {id}.",
            Details = JsonSerializer.Serialize(new { MemberId = id, From = from.Date, To = to.Date, ContributionCount = direct.Count }) }, ct);
        return Ok(new { Member = new { member.Id, member.MembershipNumber, DisplayName = DisplayName(member) }, From = from.Date, To = to.Date,
            OfficialTotal = official - reversed, ReversedTotal = reversed,
            ByType = items.Where(x => x.FinancialStatus is "CONFIRMED" or "ALLOCATED" or ServiceCollectionStatuses.Approved)
                .GroupBy(x => x.ContributionType).Select(x => new { ContributionType = x.Key, Amount = x.Sum(y => y.IsReversal ? -y.Amount : y.Amount) }), Items = items });
    }

    private async Task<string?> Validate(SaveMemberRequest request, Guid? id, CancellationToken ct)
    {
        var number = request.MembershipNumber.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(number)) return "Membership number is required.";
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName)) return "First name and surname are required.";
        if (!MembershipStatuses.All.Contains(request.Status.Trim().ToUpperInvariant())) return "Unknown membership status.";
        if (await db.Members.AnyAsync(x => x.Id != id && x.MembershipNumber.ToUpper() == number, ct)) return "Membership number already exists.";
        if (request.SectionId.HasValue && !await db.Sections.AnyAsync(x => x.Id == request.SectionId && x.IsActive, ct)) return "Select an active section.";
        if (request.GroupId.HasValue && !await db.Groups.AnyAsync(x => x.Id == request.GroupId && x.IsActive, ct)) return "Select an active group.";
        return null;
    }

    private static void Apply(Member member, SaveMemberRequest request)
    {
        member.MembershipNumber = request.MembershipNumber.Trim().ToUpperInvariant(); member.Title = Clean(request.Title);
        member.FirstName = request.FirstName.Trim(); member.MiddleNames = Clean(request.MiddleNames); member.LastName = request.LastName.Trim();
        member.PreferredName = Clean(request.PreferredName); member.Sex = Clean(request.Sex); member.DateOfBirth = request.DateOfBirth;
        member.PhoneNumber = Clean(request.PhoneNumber); member.Email = Clean(request.Email)?.ToLowerInvariant(); member.Address = Clean(request.Address);
        member.SectionId = request.SectionId; member.GroupId = request.GroupId; member.MembershipType = request.MembershipType.Trim().ToUpperInvariant();
        member.Status = request.Status.Trim().ToUpperInvariant(); member.IsActive = member.Status == MembershipStatuses.Active; member.JoinedOn = request.JoinedOn;
        member.NormalizedSearchName = $"{member.FirstName} {member.MiddleNames} {member.LastName} {member.PreferredName}".ToUpperInvariant().Replace("  ", " ").Trim();
        member.NormalizedPhoneNumber = member.PhoneNumber == null ? null : new string(member.PhoneNumber.Where(char.IsDigit).ToArray());
    }

    private void AddStatus(Member member, string previous, string next, string reason, Guid actor) => db.MembershipStatusHistory.Add(new MembershipStatusHistory
        { Id = Guid.NewGuid(), MemberId = member.Id, PreviousStatus = previous, NewStatus = next, EffectiveAtUtc = DateTime.UtcNow,
          Reason = reason, ChangedById = actor, CreatedById = actor, UpdatedById = actor });
    private void AddAudit(Guid memberId, string action, object changes, Guid actor) => db.MemberAudits.Add(new MemberAud
        { MemberId = memberId, Action = action, ChangesJson = JsonSerializer.Serialize(changes), PerformedById = actor, CorrelationId = HttpContext.TraceIdentifier });
    private static object Summary(Member x) => new { x.Id, x.MembershipNumber, DisplayName = DisplayName(x), x.PhoneNumber, x.Email, x.MembershipType, x.Status, x.IsActive, x.IsArchived, x.Version, Section = x.Section?.SectionName, Group = x.Group?.GroupName };
    private static object Detail(Member x) => new { x.Id, x.MembershipNumber, x.Title, x.FirstName, x.MiddleNames, x.LastName, x.PreferredName, x.Sex, x.DateOfBirth, x.PhoneNumber, x.Email, x.Address, x.SectionId, x.GroupId, x.MembershipType, x.Status, x.JoinedOn, x.IsActive, x.IsArchived, x.ArchiveReason, x.RecordSource, x.SynchronizationStatus, x.LastSynchronizedAtUtc, x.Version, Section = x.Section?.SectionName, Group = x.Group?.GroupName, x.ExternalIdentities, StatusHistory = x.StatusHistory.OrderByDescending(h => h.EffectiveAtUtc) };
    private static object AuditView(Member x) => new { x.MembershipNumber, x.Title, x.FirstName, x.MiddleNames, x.LastName, x.PreferredName, x.PhoneNumber, x.Email, x.SectionId, x.GroupId, x.MembershipType, x.Status, x.IsActive, x.IsArchived, x.Version };
    private static string DisplayName(Member x) => x.PreferredName ?? $"{x.FirstName} {x.LastName}".Trim();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal sealed record MemberFinancialActivityItem(Guid Id, string ContributionNumber, DateTime ContributionDate,
    string ContributionType, decimal Amount, string CurrencyCode, decimal OriginalCurrencyAmount,
    decimal ExchangeRateSnapshot, string PaymentMethod, string Status, string FinancialStatus, string? Description,
    string? ReferenceNumber, Guid? FinalizedFinancialRecordBookId, string OriginType, bool IsReversal, string NavigationPath);
