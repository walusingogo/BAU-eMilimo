using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using eMilimo.API.Authorization;
using eMilimo.API.Platform;
using eMilimo.API.Services;
using eMilimo.Core.DTOs;
using eMilimo.Core.Entities;
using eMilimo.Core.Configuration;
using eMilimo.Core.Enums;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceCollection = eMilimo.Core.Entities.ServiceCollection;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/service-collections"), Authorize]
public sealed class ServiceCollectionsController(
    AppDbContext db, ICurrentUserService current, IGroupAccessService access,
    IWorkflowAuthorizationService workflow, IReferenceNumberService references, IFileStorage storage,
    IServiceCollectionContributionPostingService serviceCollectionPosting,
    ICashDenominationSettingsService denominationSettings, IOptions<UploadSettings> uploadOptions,
    IConfiguration configuration, ISystemLogWriter logs) : ControllerBase
{
    private static readonly string[] RegisterStatuses =
    [
        ServiceCollectionStatuses.Draft, ServiceCollectionStatuses.Submitted,
        ServiceCollectionStatuses.Returned, ServiceCollectionStatuses.Approved,
        ServiceCollectionStatuses.Archived, ServiceCollectionStatuses.Reversed
    ];
    private Guid UserId() => current.GetUserId() ?? throw new UnauthorizedAccessException();

    [HttpGet, RequirePermission("service-collections.view")]
    public async Task<IActionResult> List([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? status, CancellationToken ct)
    {
        var query = db.ServiceCollections.AsNoTracking().Include(x => x.ServiceEvent).ThenInclude(x => x!.ServiceType).AsQueryable();
        var dateError = ValidateDateRange(from, to); if (dateError != null) return BadRequest(new { message = dateError });
        if (from.HasValue) { var start = LocalDateStartUtc(from.Value); query = query.Where(x => x.ServiceEvent!.StartsAtUtc >= start); }
        if (to.HasValue) { var end = LocalDateStartUtc(to.Value.Date.AddDays(1)); query = query.Where(x => x.ServiceEvent!.StartsAtUtc < end); }
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            if (!RegisterStatuses.Contains(normalizedStatus)) return BadRequest(new { message = "Select a valid Service Collection status." });
            query = query.Where(x => x.Status == normalizedStatus);
        }
        if (!access.HasGlobalAccess())
        {
            var sectionIds = await access.GetAccessibleSectionIdsAsync(); var groupIds = await access.GetAccessibleGroupIdsAsync();
            var canReviewSubmitted = await IsStewardReviewer(ct);
            query = query.Where(x => x.CreatedById == UserId() || x.SectionId.HasValue && sectionIds.Contains(x.SectionId.Value) ||
                x.GroupId.HasValue && groupIds.Contains(x.GroupId.Value) || canReviewSubmitted && x.Status == ServiceCollectionStatuses.Submitted);
        }
        return Ok(await query.OrderByDescending(x => x.ServiceEvent!.StartsAtUtc).ThenByDescending(x => x.CreatedDate)
            .Select(x => new { x.Id, x.CollectionNumber, x.Status, x.PurposeTotal, x.TenderTotal, x.Variance, x.Version,
                x.ServiceEventId, x.ServiceEvent!.StartsAtUtc, ServiceType = x.ServiceEvent.ServiceType!.Name, x.ServiceEvent.SequenceName,
                EntryCount = x.Entries.Count, x.CreatedDate, x.SubmittedAtUtc, x.ApprovedAtUtc }).ToListAsync(ct));
    }

    [HttpGet("register"), RequirePermission("service-collections.view")]
    public async Task<IActionResult> Register([FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] Guid? serviceEventId, [FromQuery] Guid? contributionSourceId,
        [FromQuery] Guid? createdById, [FromQuery] Guid? submittedById,
        [FromQuery] string? createdBy, [FromQuery] string? submittedBy,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string sortBy = "createdDate", [FromQuery] string sortDirection = "desc",
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 10, 100);
        var dateError = ValidateDateRange(from, to); if (dateError != null) return BadRequest(new { message = dateError });
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();
        if (normalizedStatus != null && !RegisterStatuses.Contains(normalizedStatus))
            return BadRequest(new { message = "Select a valid Service Collection status." });
        var query = db.ServiceCollections.AsNoTracking().AsQueryable();
        if (!access.HasGlobalAccess())
        {
            var sectionIds = await access.GetAccessibleSectionIdsAsync(); var groupIds = await access.GetAccessibleGroupIdsAsync();
            var committeeIds = await access.GetAccessibleCommitteeIdsAsync();
            var canReviewSubmitted = await IsStewardReviewer(ct);
            query = query.Where(x => x.CreatedById == UserId() ||
                x.SectionId.HasValue && sectionIds.Contains(x.SectionId.Value) ||
                x.GroupId.HasValue && groupIds.Contains(x.GroupId.Value) ||
                x.CommitteeId.HasValue && committeeIds.Contains(x.CommitteeId.Value) ||
                canReviewSubmitted && x.Status == ServiceCollectionStatuses.Submitted);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.CollectionNumber.ToLower().Contains(term) ||
                x.ServiceEvent!.ServiceType!.Name.ToLower().Contains(term) ||
                (x.ServiceEvent.SequenceName != null && x.ServiceEvent.SequenceName.ToLower().Contains(term)) ||
                x.Entries.Any(e => e.ContributorNameSnapshot.ToLower().Contains(term)) ||
                db.Users.Any(u => u.Id == x.CreatedById && (u.FirstName + " " + u.LastName).ToLower().Contains(term)) ||
                x.SubmittedById.HasValue && db.Users.Any(u => u.Id == x.SubmittedById && (u.FirstName + " " + u.LastName).ToLower().Contains(term)));
        }
        if (serviceEventId.HasValue) query = query.Where(x => x.ServiceEventId == serviceEventId);
        if (contributionSourceId.HasValue) query = query.Where(x => x.ContributionSourceId == contributionSourceId);
        if (createdById.HasValue) query = query.Where(x => x.CreatedById == createdById);
        if (submittedById.HasValue) query = query.Where(x => x.SubmittedById == submittedById);
        if (!string.IsNullOrWhiteSpace(createdBy))
        {
            var creator = createdBy.Trim().ToLower();
            query = query.Where(x => db.Users.Any(u => u.Id == x.CreatedById && (u.FirstName + " " + u.LastName).ToLower().Contains(creator)));
        }
        if (!string.IsNullOrWhiteSpace(submittedBy))
        {
            var submitter = submittedBy.Trim().ToLower();
            query = query.Where(x => x.SubmittedById.HasValue && db.Users.Any(u => u.Id == x.SubmittedById && (u.FirstName + " " + u.LastName).ToLower().Contains(submitter)));
        }
        if (from.HasValue) { var start = LocalDateStartUtc(from.Value); query = query.Where(x => x.ServiceEvent!.StartsAtUtc >= start); }
        if (to.HasValue) { var end = LocalDateStartUtc(to.Value.Date.AddDays(1)); query = query.Where(x => x.ServiceEvent!.StartsAtUtc < end); }

        var statusSummary = await query.GroupBy(x => x.Status).Select(group => new
        {
            Status = group.Key,
            Count = group.Count(),
            TotalAmount = group.Sum(x => x.PurposeTotal)
        }).ToListAsync(ct);
        var summaryTotal = statusSummary.Sum(x => x.Count);
        var summaryAmount = statusSummary.Sum(x => x.TotalAmount);
        if (normalizedStatus != null) query = query.Where(x => x.Status == normalizedStatus);

        var ascending = sortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        query = sortBy.Trim().ToLowerInvariant() switch
        {
            "collectiondate" => ascending ? query.OrderBy(x => x.ServiceEvent!.StartsAtUtc) : query.OrderByDescending(x => x.ServiceEvent!.StartsAtUtc),
            "updateddate" => ascending ? query.OrderBy(x => x.UpdatedDate) : query.OrderByDescending(x => x.UpdatedDate),
            "totalamount" => ascending ? query.OrderBy(x => x.PurposeTotal) : query.OrderByDescending(x => x.PurposeTotal),
            "collectionnumber" => ascending ? query.OrderBy(x => x.CollectionNumber) : query.OrderByDescending(x => x.CollectionNumber),
            "status" => ascending ? query.OrderBy(x => x.Status) : query.OrderByDescending(x => x.Status),
            _ => ascending ? query.OrderBy(x => x.CreatedDate) : query.OrderByDescending(x => x.CreatedDate)
        };
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
        {
            x.Id, x.CollectionNumber, x.Status, TotalAmount = x.PurposeTotal, Currency = x.BaseCurrencyCode,
            CollectionDate = x.ServiceEvent!.StartsAtUtc, ServiceEvent = x.ServiceEvent.ServiceType!.Name +
                (x.ServiceEvent.SequenceName == null || x.ServiceEvent.SequenceName == "" ? "" : " — " + x.ServiceEvent.SequenceName),
            ContributionSource = x.ContributionSource!.Name, x.ServiceEventId, x.ContributionSourceId,
            CreatedById = x.CreatedById, CreatedBy = db.Users.Where(u => u.Id == x.CreatedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
            x.SubmittedById, SubmittedBy = db.Users.Where(u => u.Id == x.SubmittedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
            x.ApprovedById, ApprovedBy = db.Users.Where(u => u.Id == x.ApprovedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
            x.CreatedDate, x.UpdatedDate, x.SubmittedAtUtc, x.ApprovedAtUtc, x.Version,
            EntryCount = x.Entries.Count
        }).ToListAsync(ct);
        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize),
            summaryTotal, summaryAmount, statusSummary });
    }

    [HttpGet("{id:guid}"), RequirePermission("service-collections.view")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var item = await Full(db.ServiceCollections.AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item == null) return NotFound(new { message = "Service Collection not found." });
        await DemandAccess(item, ct);
        return Ok(Present(item, await UserName(item.CreatedById, ct)));
    }

    [HttpGet("metadata"), RequirePermission("service-collections.view")]
    public async Task<IActionResult> Metadata(CancellationToken ct)
    {
        var actorId = UserId();
        var currentCounter = await db.Users.AsNoTracking().Where(x => x.Id == actorId && x.Status == "ACTIVE")
            .Select(x => new { x.Id, Name = x.FirstName + " " + x.LastName, HasSignature = x.SignatureData != "", x.SignatureData })
            .FirstOrDefaultAsync(ct);
        if (currentCounter == null) return Unauthorized(new { message = "Your active user profile could not be loaded." });
        var hasGlobalAccess = access.HasGlobalAccess();
        var sections = db.Sections.AsNoTracking().Where(x => x.IsActive);
        var groups = db.Groups.AsNoTracking().Where(x => x.IsActive);
        var committees = db.Committees.AsNoTracking().Where(x => x.IsActive);
        if (!hasGlobalAccess)
        {
            var sectionIds = await access.GetAccessibleSectionIdsAsync();
            var groupIds = await access.GetAccessibleGroupIdsAsync();
            var committeeIds = await access.GetAccessibleCommitteeIdsAsync();
            sections = sections.Where(x => sectionIds.Contains(x.Id));
            groups = groups.Where(x => groupIds.Contains(x.Id));
            committees = committees.Where(x => committeeIds.Contains(x.Id));
        }
        return Ok(new
        {
            HasGlobalAccess = hasGlobalAccess,
            ServiceTypes = await db.ServiceTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ToListAsync(ct),
            PaymentMethods = await db.PaymentMethodDefinitions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ToListAsync(ct),
            Currencies = await db.Currencies.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.IsBaseCurrency).ThenBy(x => x.Code).ToListAsync(ct),
            ExchangeRates = await db.ExchangeRates.AsNoTracking().Where(x => x.IsApproved).OrderByDescending(x => x.EffectiveAtUtc).Take(200)
                .Select(x => new { x.Id, x.FromCurrencyCode, x.ToCurrencyCode, x.Rate, x.EffectiveAtUtc, x.ExpiresAtUtc, x.SourceKind, x.SourceReference }).ToListAsync(ct),
            ContributionTypes = await db.ContributionTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ToListAsync(ct),
            BaseCurrencyCode = await db.FcyManagementSettings.AsNoTracking()
                .Select(x => x.BaseCurrencyCode)
                .FirstOrDefaultAsync(ct) ?? "ZMW",
            BaseCurrencyDenominations = await denominationSettings.GetAsync(ct),
            Sources = await db.ContributionSources.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct),
            BankAccounts = await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.BankName)
                .Select(x => new { x.Id, Name = x.BankName + " — " + x.AccountName + " (" + x.AccountNumber + ")" }).ToListAsync(ct),
            CurrentCounter = currentCounter,
            Sections = await sections.OrderBy(x => x.SectionName).Select(x => new { x.Id, Name = x.SectionName }).ToListAsync(ct),
            Groups = await groups.OrderBy(x => x.GroupName).Select(x => new { x.Id, Name = x.GroupName }).ToListAsync(ct),
            Committees = await committees.OrderBy(x => x.CommitteeName).Select(x => new { x.Id, Name = x.CommitteeName }).ToListAsync(ct)
        });
    }

    [HttpGet("events"), RequirePermission("service-collections.view")]
    public async Task<IActionResult> Events([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var query = db.ServiceEvents.AsNoTracking().Include(x => x.ServiceType).AsQueryable();
        if (!includeInactive) query = query.Where(x => x.Status == ServiceEventStatuses.Active);
        if (from.HasValue) query = query.Where(x => x.StartsAtUtc >= from.Value.Date);
        if (to.HasValue) { var end = to.Value.Date.AddDays(1); query = query.Where(x => x.StartsAtUtc < end); }
        if (!access.HasGlobalAccess())
        {
            var sectionIds = await access.GetAccessibleSectionIdsAsync(); var groupIds = await access.GetAccessibleGroupIdsAsync();
            var committeeIds = await access.GetAccessibleCommitteeIdsAsync();
            query = query.Where(x => x.CreatedById == UserId() || x.SectionId.HasValue && sectionIds.Contains(x.SectionId.Value) ||
                x.GroupId.HasValue && groupIds.Contains(x.GroupId.Value) || x.CommitteeId.HasValue && committeeIds.Contains(x.CommitteeId.Value));
        }
        return Ok(await query.OrderByDescending(x => x.StartsAtUtc).Take(200).ToListAsync(ct));
    }

    [HttpGet("events/maintenance"), RequirePermission("settings.manage")]
    public async Task<IActionResult> EventMaintenance([FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 10, 100);
        var query = db.ServiceEvents.AsNoTracking().Include(x => x.ServiceType).Include(x => x.Section)
            .Include(x => x.Group).Include(x => x.Committee).AsQueryable();
        var normalizedStatus = (status ?? "ALL").Trim().ToUpperInvariant();
        if (normalizedStatus != "ALL") query = query.Where(x => x.Status == normalizedStatus);
        var term = Clean(search)?.ToLowerInvariant();
        if (term != null) query = query.Where(x => x.ServiceType!.Name.ToLower().Contains(term) ||
            (x.SequenceName != null && x.SequenceName.ToLower().Contains(term)) ||
            (x.Location != null && x.Location.ToLower().Contains(term)) ||
            (x.OfficiatingMinister != null && x.OfficiatingMinister.ToLower().Contains(term)));
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.StartsAtUtc).ThenBy(x => x.ServiceType!.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
            {
                x.Id, x.ServiceTypeId, ServiceType = x.ServiceType!.Name, x.StartsAtUtc, x.SequenceName,
                x.Description, x.Location, x.SectionId, Section = x.Section != null ? x.Section.SectionName : null,
                x.GroupId, Group = x.Group != null ? x.Group.GroupName : null,
                x.CommitteeId, Committee = x.Committee != null ? x.Committee.CommitteeName : null,
                x.OfficiatingMinister, x.Status, x.Version,
                CollectionCount = db.ServiceCollections.Count(collection => collection.ServiceEventId == x.Id)
            }).ToListAsync(ct);
        return Ok(new { Data = rows, Total = total, Page = page, PageSize = pageSize });
    }

    [HttpPost("events/{id:guid}/status"), RequirePermission("settings.manage")]
    public async Task<IActionResult> ChangeEventStatus(Guid id, ServiceEventStatusRequest request, CancellationToken ct)
    {
        var next = (request.Status ?? "").Trim().ToUpperInvariant();
        if (next is not (ServiceEventStatuses.Active or ServiceEventStatuses.Disabled or ServiceEventStatuses.Archived))
            return BadRequest(new { message = "Select Active, Disabled, or Archived." });
        var item = await db.ServiceEvents.Include(x => x.ServiceType).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item == null) return NotFound(new { message = "Service event not found." });
        if (item.Version != request.Version) return Conflict(new { message = "This service event changed while you were working. Reload and try again.", currentVersion = item.Version });
        if (item.Status == ServiceEventStatuses.Archived && next != ServiceEventStatuses.Archived)
            return BadRequest(new { message = "An archived service event cannot be reactivated." });
        var reason = Clean(request.Reason);
        if (next != ServiceEventStatuses.Active && reason == null)
            return BadRequest(new { message = "Provide a reason for disabling or archiving this service event." });
        var previous = item.Status;
        if (previous == next) return Ok(item);
        var actor = UserId(); item.Status = next; item.UpdatedById = actor; item.UpdatedDate = DateTime.UtcNow; item.Version++;
        await db.SaveChangesAsync(ct);
        await logs.WriteAsync(new SystemLog
        {
            Level = "Information", Category = "Audit", Process = "Service Event Maintenance",
            Action = next == ServiceEventStatuses.Active ? "Activated" : next == ServiceEventStatuses.Disabled ? "Disabled" : "Archived",
            UserId = actor, CorrelationId = HttpContext.TraceIdentifier,
            Message = JsonSerializer.Serialize(new { ServiceEventId = item.Id, ServiceType = item.ServiceType?.Name,
                PreviousStatus = previous, NewStatus = next, Reason = reason })
        }, ct);
        return Ok(new { item.Id, item.Status, item.Version, item.UpdatedDate });
    }

    [HttpPost("events"), RequirePermission("service-collections.create")]
    public async Task<IActionResult> CreateEvent(SaveServiceEventRequest request, CancellationToken ct)
    {
        if (!await db.ServiceTypes.AnyAsync(x => x.Id == request.ServiceTypeId && x.IsActive, ct)) return BadRequest(new { message = "Select an active service type." });
        var scopeError = await ValidateScope(request.SectionId, request.GroupId, request.CommitteeId, ct); if (scopeError != null) return BadRequest(new { message = scopeError });
        if (request.Attendance < 0 || request.ChildrenAttendance < 0 || request.TeacherAttendance < 0) return BadRequest(new { message = "Attendance values cannot be negative." });
        var actor = UserId(); var item = new ServiceEvent { Id = Guid.NewGuid(), ServiceTypeId = request.ServiceTypeId,
            StartsAtUtc = request.StartsAtUtc.ToUniversalTime(), SequenceName = Clean(request.SequenceName), Description = Clean(request.Description),
            Location = Clean(request.Location), SectionId = request.SectionId, GroupId = request.GroupId, CommitteeId = request.CommitteeId,
            Attendance = request.Attendance, ChildrenAttendance = request.ChildrenAttendance, TeacherAttendance = request.TeacherAttendance,
            OfficiatingMinister = Clean(request.OfficiatingMinister), CreatedById = actor, UpdatedById = actor };
        db.ServiceEvents.Add(item); await db.SaveChangesAsync(ct);
        return Ok(await db.ServiceEvents.AsNoTracking().Include(x => x.ServiceType).FirstAsync(x => x.Id == item.Id, ct));
    }

    [HttpPut("events/{id:guid}"), RequirePermission("service-collections.edit")]
    public async Task<IActionResult> UpdateEvent(Guid id, SaveServiceEventRequest request, CancellationToken ct)
    {
        var item = await db.ServiceEvents.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item == null) return NotFound(new { message = "Service event not found." });
        if (item.Status is ServiceEventStatuses.Archived or "CANCELLED") return BadRequest(new { message = "An archived service event cannot be edited." });
        if (!request.Version.HasValue || request.Version.Value != item.Version)
            return Conflict(new { message = "This service event changed while you were editing. Reload and try again.", currentVersion = item.Version, item.UpdatedDate });
        if (!await db.ServiceTypes.AnyAsync(x => x.Id == request.ServiceTypeId && x.IsActive, ct))
            return BadRequest(new { message = "Select an active service type." });
        var scopeError = await ValidateScope(request.SectionId, request.GroupId, request.CommitteeId, ct);
        if (scopeError != null) return BadRequest(new { message = scopeError });
        if (request.Attendance < 0 || request.ChildrenAttendance < 0 || request.TeacherAttendance < 0)
            return BadRequest(new { message = "Attendance values cannot be negative." });

        var linkedCollections = await db.ServiceCollections.Where(x => x.ServiceEventId == id).ToListAsync(ct);
        if (linkedCollections.Any(x => x.Status is not (ServiceCollectionStatuses.Draft or ServiceCollectionStatuses.Returned)))
            return BadRequest(new { message = "This service event is linked to a submitted or completed collection and can no longer be edited." });

        var actor = UserId();
        var before = new { item.ServiceTypeId, item.StartsAtUtc, item.SequenceName, item.Description, item.Location,
            item.SectionId, item.GroupId, item.CommitteeId, item.Attendance, item.ChildrenAttendance,
            item.TeacherAttendance, item.OfficiatingMinister, item.Version };
        item.ServiceTypeId = request.ServiceTypeId;
        item.StartsAtUtc = request.StartsAtUtc.ToUniversalTime();
        item.SequenceName = Clean(request.SequenceName);
        item.Description = Clean(request.Description);
        item.Location = Clean(request.Location);
        item.SectionId = request.SectionId;
        item.GroupId = request.GroupId;
        item.CommitteeId = request.CommitteeId;
        item.Attendance = request.Attendance;
        item.ChildrenAttendance = request.ChildrenAttendance;
        item.TeacherAttendance = request.TeacherAttendance;
        item.OfficiatingMinister = Clean(request.OfficiatingMinister);
        item.UpdatedById = actor;
        item.UpdatedDate = DateTime.UtcNow;
        item.Version++;
        foreach (var collection in linkedCollections)
            AddAudit(collection.Id, "SERVICE_EVENT_UPDATED", new { Before = before, After = new { item.ServiceTypeId, item.StartsAtUtc,
                item.SequenceName, item.Description, item.Location, item.SectionId, item.GroupId, item.CommitteeId,
                item.Attendance, item.ChildrenAttendance, item.TeacherAttendance, item.OfficiatingMinister, item.Version } }, actor);
        await db.SaveChangesAsync(ct);
        return Ok(await db.ServiceEvents.AsNoTracking().Include(x => x.ServiceType).FirstAsync(x => x.Id == id, ct));
    }

    [HttpPost, RequirePermission("service-collections.create")]
    public async Task<IActionResult> Create(SaveServiceCollectionRequest request, CancellationToken ct)
    {
        var error = await ValidateHeader(request, null, ct); if (error != null) return BadRequest(new { message = error });
        var actor = UserId(); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var settings = await db.FcyManagementSettings.AsNoTracking().SingleAsync(ct);
        var item = new ServiceCollection { Id = Guid.NewGuid(), CollectionNumber = await references.NextAsync("SERVICE_COLLECTION", cancellationToken: ct),
            ServiceEventId = request.ServiceEventId, ContributionSourceId = request.ContributionSourceId, SectionId = request.SectionId,
            GroupId = request.GroupId, CommitteeId = request.CommitteeId, BaseCurrencyCode = settings.BaseCurrencyCode,
            Notes = Clean(request.Notes), VarianceExplanation = Clean(request.VarianceExplanation), CreatedById = actor, UpdatedById = actor };
        db.ServiceCollections.Add(item);
        var captureError = await ReplaceCapture(item, request, actor, ct); if (captureError != null) return BadRequest(new { message = captureError });
        Recalculate(item); AddAudit(item.Id, "CREATED", CaptureAudit(item), actor);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "The Service Collection changed while it was being created. Please try again." }); }
        await tx.CommitAsync(ct); return Ok(await Reload(item.Id, ct));
    }

    [HttpPut("{id:guid}"), RequirePermission("service-collections.edit")]
    public async Task<IActionResult> Update(Guid id, SaveServiceCollectionRequest request, CancellationToken ct)
    {
        var snapshot = await Full(db.ServiceCollections.AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (snapshot == null) return NotFound(new { message = "Service Collection not found." });
        await DemandAccess(snapshot, ct);
        if (snapshot.Status is not (ServiceCollectionStatuses.Draft or ServiceCollectionStatuses.Returned)) return BadRequest(new { message = "Only draft or returned collections can be edited." });
        if (!request.Version.HasValue || request.Version != snapshot.Version) return Conflict(new
        {
            message = "This collection changed while you were editing. Reload and try again.",
            currentVersion = snapshot.Version,
            snapshot.UpdatedDate
        });
        var error = await ValidateHeader(request, id, ct); if (error != null) return BadRequest(new { message = error });
        var actor = UserId(); var before = CaptureAudit(snapshot);
        snapshot.Entries.Clear(); snapshot.Denominations.Clear(); snapshot.Tenders.Clear(); snapshot.Participants.Clear();
        var captureError = await ReplaceCapture(snapshot, request, actor, ct); if (captureError != null) return BadRequest(new { message = captureError });
        Recalculate(snapshot);

        var expectedVersion = request.Version.Value;
        var nextVersion = expectedVersion + 1;
        var updatedAt = DateTime.UtcNow;
        var notes = Clean(request.Notes);
        var varianceExplanation = Clean(request.VarianceExplanation);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // The collection is the aggregate concurrency boundary. Advance its version with one
        // atomic UPDATE; child rows are replacement data and must not manufacture false 409s.
        var affected = await db.ServiceCollections
            .Where(x => x.Id == id && x.Version == expectedVersion &&
                (x.Status == ServiceCollectionStatuses.Draft || x.Status == ServiceCollectionStatuses.Returned))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ServiceEventId, request.ServiceEventId)
                .SetProperty(x => x.ContributionSourceId, request.ContributionSourceId)
                .SetProperty(x => x.SectionId, request.SectionId)
                .SetProperty(x => x.GroupId, request.GroupId)
                .SetProperty(x => x.CommitteeId, request.CommitteeId)
                .SetProperty(x => x.Notes, notes)
                .SetProperty(x => x.VarianceExplanation, varianceExplanation)
                .SetProperty(x => x.PurposeTotal, snapshot.PurposeTotal)
                .SetProperty(x => x.TenderTotal, snapshot.TenderTotal)
                .SetProperty(x => x.Variance, snapshot.Variance)
                .SetProperty(x => x.UpdatedById, actor)
                .SetProperty(x => x.UpdatedDate, updatedAt)
                .SetProperty(x => x.Version, nextVersion), ct);

        if (affected == 0)
        {
            await tx.RollbackAsync(ct);
            var currentState = await db.ServiceCollections.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new { x.Status, x.Version, x.UpdatedDate })
                .FirstOrDefaultAsync(ct);
            if (currentState == null) return NotFound(new { message = "Service Collection not found." });
            if (currentState.Status is not (ServiceCollectionStatuses.Draft or ServiceCollectionStatuses.Returned))
                return BadRequest(new { message = "Only draft or returned collections can be edited." });
            return Conflict(new
            {
                message = "This collection changed while you were editing. Reload and try again.",
                currentVersion = currentState.Version,
                currentState.UpdatedDate
            });
        }

        await DeleteCapture(id, ct);
        db.ServiceCollectionEntries.AddRange(snapshot.Entries);
        db.ServiceCollectionDenominations.AddRange(snapshot.Denominations);
        db.ServiceCollectionTenders.AddRange(snapshot.Tenders);
        db.ServiceCollectionParticipants.AddRange(snapshot.Participants);
        snapshot.ServiceEventId = request.ServiceEventId; snapshot.ContributionSourceId = request.ContributionSourceId;
        snapshot.SectionId = request.SectionId; snapshot.GroupId = request.GroupId; snapshot.CommitteeId = request.CommitteeId;
        snapshot.Notes = notes; snapshot.VarianceExplanation = varianceExplanation; snapshot.UpdatedById = actor;
        snapshot.UpdatedDate = updatedAt; snapshot.Version = nextVersion;
        AddAudit(id, "UPDATED", new { Before = before, After = CaptureAudit(snapshot) }, actor);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/submit"), RequirePermission("service-collections.submit")]
    public async Task<IActionResult> Submit(Guid id, ServiceCollectionDecisionRequest request, CancellationToken ct)
    {
        var item = await Full(db.ServiceCollections).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item == null) return NotFound(new { message = "Service Collection not found." });
        await DemandAccess(item, ct);
        if (item.Status is not (ServiceCollectionStatuses.Draft or ServiceCollectionStatuses.Returned)) return BadRequest(new { message = "Only draft or returned collections can be submitted." });
        if (item.Version != request.Version) return Conflict(new { message = "This collection changed. Reload and try again.", currentVersion = item.Version, item.UpdatedDate });
        Recalculate(item); var validation = await ValidateForSubmission(item, ct); if (validation != null) return BadRequest(new { message = validation });
        var actor = UserId();
        var signer = await db.Users.AsNoTracking().Where(x => x.Id == actor && x.Status == "ACTIVE")
            .Select(x => new { x.SignatureData }).FirstOrDefaultAsync(ct);
        if (signer == null) return Unauthorized(new { message = "Your active user profile could not be loaded." });
        if (string.IsNullOrWhiteSpace(signer.SignatureData)) return BadRequest(new { message = "Add your digital signature to your profile before submitting a Service Collection." });
        var counter = item.Participants.FirstOrDefault(x => x.UserId == actor && x.Responsibility == "COUNTER");
        if (counter == null) return BadRequest(new { message = "The authenticated user must be recorded as the collection counter." });
        counter.SignatureData = signer.SignatureData; counter.CompletedAtUtc = DateTime.UtcNow; counter.UpdatedById = actor; counter.UpdatedDate = DateTime.UtcNow; counter.Version++;
        item.Status = ServiceCollectionStatuses.Submitted; item.SubmittedAtUtc = DateTime.UtcNow; item.SubmittedById = actor;
        item.ReviewComments = null; item.UpdatedById = actor; item.UpdatedDate = DateTime.UtcNow; item.Version++;
        db.ServiceCollectionLocks.Add(new ServiceCollectionLck { Id = Guid.NewGuid(), EntityId = id, LockedById = actor,
            LockedAt = DateTime.UtcNow, LockReason = "Submitted for review.", LockType = "WORKFLOW", CreatedById = actor, UpdatedById = actor });
        AddAudit(id, "SUBMITTED", new { item.PurposeTotal, item.TenderTotal, item.Variance, Comments = Clean(request.Comments) }, actor);
        await db.SaveChangesAsync(ct); return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/return"), RequirePermission("service-collections.review")]
    public async Task<IActionResult> Return(Guid id, ServiceCollectionDecisionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Comments)) return BadRequest(new { message = "Return comments are required." });
        var item = await db.ServiceCollections.FirstOrDefaultAsync(x => x.Id == id, ct); if (item == null) return NotFound(new { message = "Service Collection not found." });
        if (item.Status != ServiceCollectionStatuses.Submitted) return BadRequest(new { message = "Only a submitted collection can be returned." });
        if (item.Version != request.Version) return Conflict(new { message = "This collection changed. Reload and try again.", currentVersion = item.Version, item.UpdatedDate });
        if (!await CanReview(item, WorkflowCapabilities.ServiceCollectionReview, ct)) return Forbid();
        var actor = UserId(); item.Status = ServiceCollectionStatuses.Returned; item.ReturnedAtUtc = DateTime.UtcNow; item.ReturnedById = actor;
        item.ReviewComments = request.Comments.Trim(); item.UpdatedById = actor; item.UpdatedDate = DateTime.UtcNow; item.Version++;
        await ReleaseLocks(id, actor, ct); AddAudit(id, "RETURNED", new { request.Comments }, actor); await db.SaveChangesAsync(ct); return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/approve"), RequirePermission("service-collections.review")]
    public async Task<IActionResult> Approve(Guid id, ServiceCollectionDecisionRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Approval reads a complete snapshot for validation, but does not track the
        // aggregate graph. Tracking every entry, line, tender and participant made
        // an otherwise valid approval vulnerable to an unrelated child-entity
        // concurrency update when SaveChanges processed the whole graph.
        var item = await Full(db.ServiceCollections.AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id, ct); if (item == null) return NotFound(new { message = "Service Collection not found." });
        if (item.Status != ServiceCollectionStatuses.Submitted) return BadRequest(new { message = "Only a submitted collection can be approved." });
        if (item.Version != request.Version) return Conflict(new { message = "This collection changed. Reload and try again.", currentVersion = item.Version, item.UpdatedDate });
        if (!await CanReview(item, WorkflowCapabilities.ServiceCollectionReview, ct)) return Forbid();
        var actor = UserId();
        var superAdminOverride = string.Equals(current.GetUserRole(), "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        if (item.SubmittedById == actor && !superAdminOverride)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Separation of duties prevents the submitting Steward from approving the same Service Collection." });
        var checker = await db.Users.AsNoTracking().Where(x => x.Id == actor && x.Status == "ACTIVE")
            .Select(x => new { x.SignatureData }).FirstOrDefaultAsync(ct);
        if (checker == null) return Unauthorized(new { message = "Your active user profile could not be loaded." });
        if (string.IsNullOrWhiteSpace(checker.SignatureData)) return BadRequest(new { message = "Add your digital signature to your profile before approving a Service Collection." });
        Recalculate(item); var validation = await ValidateForSubmission(item, ct); if (validation != null) return BadRequest(new { message = validation });
        if (item.Variance != 0 && !await CanReview(item, WorkflowCapabilities.ServiceCollectionVarianceApprove, ct))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "This collection has a variance and requires a variance-approval capability in scope." });
        await serviceCollectionPosting.PostAsync(item, actor, ct);
        var approvedAt = DateTime.UtcNow;
        var reviewComments = Clean(request.Comments);
        var affected = await db.ServiceCollections
            .Where(x => x.Id == id && x.Status == ServiceCollectionStatuses.Submitted && x.Version == request.Version)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ServiceCollectionStatuses.Approved)
                .SetProperty(x => x.ApprovedAtUtc, approvedAt)
                .SetProperty(x => x.ApprovedById, actor)
                .SetProperty(x => x.ReviewComments, reviewComments)
                .SetProperty(x => x.UpdatedById, actor)
                .SetProperty(x => x.UpdatedDate, approvedAt)
                .SetProperty(x => x.Version, request.Version + 1), ct);
        if (affected == 0)
        {
            await tx.RollbackAsync(ct);
            var currentState = await db.ServiceCollections.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new { x.Status, x.Version, x.UpdatedDate }).FirstOrDefaultAsync(ct);
            return Conflict(new
            {
                message = "This collection changed before approval could be completed. Reload and try again.",
                currentStatus = currentState?.Status,
                currentVersion = currentState?.Version,
                currentUpdatedDate = currentState?.UpdatedDate
            });
        }
        db.ServiceCollectionParticipants.Add(new ServiceCollectionParticipant { Id = Guid.NewGuid(), ServiceCollectionId = item.Id, UserId = actor,
            Responsibility = "CHECKER", SignatureData = checker.SignatureData, CompletedAtUtc = DateTime.UtcNow,
            CreatedById = actor, UpdatedById = actor });
        await ReleaseLocks(id, actor, ct);
        AddAudit(id, "APPROVED", new { item.PurposeTotal, item.TenderTotal, item.Variance, request.Comments,
            SuperAdminOverride = superAdminOverride && item.SubmittedById == actor }, actor);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/reverse"), RequirePermission("service-collections.review")]
    public async Task<IActionResult> Reverse(Guid id, ServiceCollectionReverseRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { message = "A reversal reason is required." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var item = await Full(db.ServiceCollections).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item == null) return NotFound(new { message = "Service Collection not found." });
        if (item.Status != ServiceCollectionStatuses.Approved) return BadRequest(new { message = "Only an approved Service Collection can be reversed." });
        if (item.Version != request.Version) return Conflict(new { message = "This collection changed. Reload and try again.", currentVersion = item.Version, item.UpdatedDate });
        if (item.FinalizedFinancialRecordBookId.HasValue)
            return Conflict(new { message = "This collection belongs to a finalised Financial Record Book and cannot be reversed. Use an authorised adjustment process." });
        if (!await CanReview(item, WorkflowCapabilities.ServiceCollectionReview, ct)) return Forbid();

        var actor = UserId();
        var lines = await db.ServiceCollectionEntryLines
            .Where(x => x.ServiceCollectionEntry!.ServiceCollectionId == id)
            .OrderBy(x => x.CreatedDate).ThenBy(x => x.Id)
            .ToListAsync(ct);
        if (lines.Count == 0 || lines.Any(x => !x.ContributionId.HasValue))
            return Conflict(new { message = "This historical collection is not fully linked to the central Contributions register. Reconcile it before attempting a reversal." });
        var contributionIds = lines.Select(x => x.ContributionId!.Value).Distinct().ToArray();
        var contributions = await db.Contributions.Where(x => contributionIds.Contains(x.Id)).ToListAsync(ct);
        if (contributions.Count != contributionIds.Length)
            return Conflict(new { message = "One or more central Contribution records could not be found. No balances were changed." });
        var postings = await db.FundTransactions.Where(x => x.ReferenceType == "CONTRIBUTION" && x.ReferenceId.HasValue &&
                contributionIds.Contains(x.ReferenceId.Value) && !x.IsReversed)
            .OrderBy(x => x.CreatedDate).ThenBy(x => x.Id).ToListAsync(ct);
        if (item.PurposeTotal > 0 && postings.Count == 0)
            return Conflict(new { message = "No reversible central Contribution posting could be matched to this approved collection. No balances were changed." });

        var reason = request.Reason.Trim();
        var reversalIds = new List<Guid>();
        foreach (var posting in postings)
        {
            var fiscalYear = posting.CreatedDate.Year;
            var balance = await db.FundBalances.FirstOrDefaultAsync(x => x.FiscalYear == fiscalYear && x.SectionId == posting.SectionId &&
                x.GroupId == posting.GroupId && x.CommitteeId == posting.CommitteeId, ct);
            if (balance == null)
                return Conflict(new { message = $"The fund balance for posting {posting.Id} could not be found. No reversal was applied." });
            var previous = balance.OwnFundsBalance;
            balance.OwnFundsBalance = Money(previous - posting.Amount);
            balance.UpdatedById = actor; balance.UpdatedDate = DateTime.UtcNow; balance.Version++;
            posting.IsReversed = true; posting.ReversedBy = actor; posting.ReversedAt = DateTime.UtcNow;
            posting.ReversalReason = reason; posting.UpdatedById = actor; posting.UpdatedDate = DateTime.UtcNow; posting.Version++;
            var reversalId = Guid.NewGuid(); reversalIds.Add(reversalId);
            db.FundTransactions.Add(new FundTransaction
            {
                Id = reversalId, SectionId = posting.SectionId, GroupId = posting.GroupId, CommitteeId = posting.CommitteeId,
                TransactionType = "SUBTRACT", FundType = posting.FundType, Amount = -posting.Amount,
                PreviousBalance = previous, NewBalance = balance.OwnFundsBalance,
                Description = $"Reversal of Service Collection {item.CollectionNumber}", Source = item.CollectionNumber,
                ReferenceType = "SERVICE_COLLECTION_REVERSAL", ReferenceId = item.Id,
                Metadata = JsonSerializer.Serialize(new { OriginalFundTransactionId = posting.Id, ContributionId = posting.ReferenceId, Reason = reason }),
                CreatedById = actor, UpdatedById = actor
            });
        }
        foreach (var contribution in contributions)
        {
            contribution.IsArchived = true;
            contribution.ArchivedAt = DateTime.UtcNow;
            contribution.ArchivedBy = actor;
            contribution.ArchiveReason = reason;
            contribution.UpdatedById = actor;
            contribution.UpdatedDate = DateTime.UtcNow;
            contribution.Version++;
        }
        item.Status = ServiceCollectionStatuses.Reversed; item.UpdatedById = actor; item.UpdatedDate = DateTime.UtcNow; item.Version++;
        AddAudit(id, "REVERSED", new { Reason = reason, ContributionIds = contributionIds,
            OriginalFundTransactionIds = postings.Select(x => x.Id).ToArray(), ReversalFundTransactionIds = reversalIds.ToArray() }, actor);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Ok(await Reload(id, ct));
    }

    [HttpPost("{id:guid}/archive"), RequirePermission("service-collections.edit")]
    public async Task<IActionResult> Archive(Guid id, ServiceCollectionArchiveRequest request, CancellationToken ct)
    {
        var item = await db.ServiceCollections.FirstOrDefaultAsync(x => x.Id == id, ct); if (item == null) return NotFound(new { message = "Service Collection not found." });
        await DemandAccess(item, ct);
        if (item.Status is not (ServiceCollectionStatuses.Draft or ServiceCollectionStatuses.Returned)) return BadRequest(new { message = "Only draft or returned collections can be archived." });
        if (item.Version != request.Version) return Conflict(new { message = "This collection changed. Reload and try again.", currentVersion = item.Version, item.UpdatedDate });
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { message = "An archive reason is required." });
        var actor = UserId(); item.Status = ServiceCollectionStatuses.Archived; item.ArchivedAtUtc = DateTime.UtcNow; item.ArchivedById = actor;
        item.ArchiveReason = request.Reason.Trim(); item.UpdatedById = actor; item.UpdatedDate = DateTime.UtcNow; item.Version++;
        AddAudit(id, "ARCHIVED", new { request.Reason }, actor); await db.SaveChangesAsync(ct); return Ok(await Reload(id, ct));
    }

    [HttpGet("{id:guid}/attachments"), RequirePermission("service-collections.view")]
    public async Task<IActionResult> Attachments(Guid id, CancellationToken ct)
    {
        var item = await db.ServiceCollections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct); if (item == null) return NotFound(); await DemandAccess(item, ct);
        return Ok(await db.ServiceCollectionAttachments.AsNoTracking().Where(x => x.EntityId == id).OrderByDescending(x => x.UploadedAt)
            .Select(x => new { x.Id, x.FileName, x.MimeType, x.Size, x.DocumentType, x.ContentHash, x.UploadedAt, x.UploadedById }).ToListAsync(ct));
    }

    [HttpPost("{id:guid}/attachments"), RequirePermission("service-collections.edit"), Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file, [FromForm] string documentType = "SUPPORTING_EVIDENCE", CancellationToken ct = default)
    {
        var item = await db.ServiceCollections.FirstOrDefaultAsync(x => x.Id == id, ct); if (item == null) return NotFound(); await DemandAccess(item, ct);
        if (item.Status is not (ServiceCollectionStatuses.Draft or ServiceCollectionStatuses.Returned)) return BadRequest(new { message = "Attachments can only be changed while a collection is draft or returned." });
        var uploadSettings = uploadOptions.Value;
        if (file.Length <= 0 || file.Length > uploadSettings.MaximumFileSizeBytes)
            return BadRequest(new { message = $"Attachment must be between 1 byte and {uploadSettings.MaximumFileSizeBytes / 1024 / 1024} MB." });
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!uploadSettings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) ||
            !uploadSettings.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = $"Unsupported attachment type. Allowed extensions: {string.Join(", ", uploadSettings.AllowedExtensions)}." });
        var normalizedType = documentType.Trim().ToUpperInvariant();
        if (normalizedType is not ("SUPPORTING_EVIDENCE" or "PAYMENT_PROOF" or "BANK_SLIP" or "COUNT_SHEET"))
            return BadRequest(new { message = "Select a valid document type." });
        await using var source = file.OpenReadStream(); var hash = Convert.ToHexString(await SHA256.HashDataAsync(source, ct)).ToLowerInvariant();
        if (await db.ServiceCollectionAttachments.AnyAsync(x => x.EntityId == id && x.ContentHash == hash, ct)) return Conflict(new { message = "This file is already attached to the collection." });
        source.Position = 0; var key = $"uploads/service-collections/{id}/{Guid.NewGuid()}{extension}"; await storage.SaveAsync(key, source, file.ContentType, ct);
        var actor = UserId(); var attachment = new ServiceCollectionAtm { Id = Guid.NewGuid(), EntityId = id, FileName = Path.GetFileName(file.FileName), FilePath = key,
            MimeType = file.ContentType, Size = file.Length, DocumentType = normalizedType, ContentHash = hash,
            UploadedById = actor, UploadedAt = DateTime.UtcNow, CreatedById = actor, UpdatedById = actor };
        db.ServiceCollectionAttachments.Add(attachment); AddAudit(id, "ATTACHMENT_ADDED", new { attachment.Id, attachment.FileName, attachment.DocumentType, attachment.ContentHash }, actor);
        await db.SaveChangesAsync(ct); return Ok(new { attachment.Id, attachment.FileName, attachment.MimeType, attachment.Size, attachment.DocumentType, attachment.ContentHash, attachment.UploadedAt });
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/file"), RequirePermission("service-collections.view")]
    public async Task<IActionResult> AttachmentFile(Guid id, Guid attachmentId, CancellationToken ct)
    {
        var item = await db.ServiceCollections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct); if (item == null) return NotFound(); await DemandAccess(item, ct);
        var attachment = await db.ServiceCollectionAttachments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == attachmentId && x.EntityId == id, ct); if (attachment == null) return NotFound();
        var path = storage.GetPhysicalPath(attachment.FilePath); return System.IO.File.Exists(path) ? PhysicalFile(path, attachment.MimeType, attachment.FileName) : NotFound();
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}"), RequirePermission("service-collections.edit")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken ct)
    {
        var item = await db.ServiceCollections.FirstOrDefaultAsync(x => x.Id == id, ct); if (item == null) return NotFound(); await DemandAccess(item, ct);
        if (item.Status is not (ServiceCollectionStatuses.Draft or ServiceCollectionStatuses.Returned)) return BadRequest(new { message = "Attachments can only be changed while a collection is draft or returned." });
        var attachment = await db.ServiceCollectionAttachments.FirstOrDefaultAsync(x => x.Id == attachmentId && x.EntityId == id, ct); if (attachment == null) return NotFound();
        await storage.DeleteAsync(attachment.FilePath, ct); db.ServiceCollectionAttachments.Remove(attachment); AddAudit(id, "ATTACHMENT_REMOVED", new { attachment.Id, attachment.FileName, attachment.ContentHash }, UserId());
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("payment-methods"), RequirePermission("settings.manage")]
    public async Task<IActionResult> PaymentMethods(CancellationToken ct) => Ok(await db.PaymentMethodDefinitions.AsNoTracking().OrderBy(x => x.DisplayOrder).ToListAsync(ct));

    [HttpPost("payment-methods"), RequirePermission("settings.manage")]
    public async Task<IActionResult> CreatePaymentMethod(SavePaymentMethodDefinitionRequest request, CancellationToken ct)
    {
        var error = ValidatePaymentMethod(request); if (error != null) return BadRequest(new { message = error });
        var code = request.Code.Trim().ToUpperInvariant(); if (await db.PaymentMethodDefinitions.AnyAsync(x => x.Code == code, ct)) return Conflict(new { message = "Payment method code already exists." });
        var actor = UserId(); var item = new PaymentMethodDefinition { Id = Guid.NewGuid(), Code = code, Name = request.Name.Trim(), Category = request.Category.Trim().ToUpperInvariant(),
            RequiresReference = request.RequiresReference, RequiresBankAccount = request.RequiresBankAccount, RequiresAttachment = request.RequiresAttachment,
            SupportsForeignCurrency = request.SupportsForeignCurrency, IsActive = request.IsActive, DisplayOrder = request.DisplayOrder, CreatedById = actor, UpdatedById = actor };
        db.PaymentMethodDefinitions.Add(item); await db.SaveChangesAsync(ct); return Ok(item);
    }

    [HttpPut("payment-methods/{id:guid}"), RequirePermission("settings.manage")]
    public async Task<IActionResult> UpdatePaymentMethod(Guid id, SavePaymentMethodDefinitionRequest request, CancellationToken ct)
    {
        var item = await db.PaymentMethodDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct); if (item == null) return NotFound(new { message = "Payment method not found." });
        if (request.Version != item.Version) return Conflict(new { message = "This payment method changed. Reload and try again." });
        var error = ValidatePaymentMethod(request); if (error != null) return BadRequest(new { message = error });
        if (!item.Code.Equals(request.Code.Trim(), StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "Payment method code cannot be changed after creation." });
        item.Name = request.Name.Trim(); item.Category = request.Category.Trim().ToUpperInvariant(); item.RequiresReference = request.RequiresReference;
        item.RequiresBankAccount = request.RequiresBankAccount; item.RequiresAttachment = request.RequiresAttachment; item.SupportsForeignCurrency = request.SupportsForeignCurrency;
        item.IsActive = request.IsActive; item.DisplayOrder = request.DisplayOrder; item.UpdatedById = UserId(); item.UpdatedDate = DateTime.UtcNow; item.Version++;
        await db.SaveChangesAsync(ct); return Ok(item);
    }

    private async Task<string?> ReplaceCapture(ServiceCollection item, SaveServiceCollectionRequest request, Guid actor, CancellationToken ct)
    {
        var typeIds = request.Entries.SelectMany(x => x.Lines).Select(x => x.ContributionTypeId).Distinct().ToArray();
        if (await db.ContributionTypes.CountAsync(x => typeIds.Contains(x.Id) && x.IsActive, ct) != typeIds.Length) return "One or more contribution types are inactive or missing.";
        var memberIds = request.Entries.Where(x => x.MemberId.HasValue).Select(x => x.MemberId!.Value).Distinct().ToArray();
        var members = await db.Members.AsNoTracking().Where(x => memberIds.Contains(x.Id) && !x.IsArchived).ToDictionaryAsync(x => x.Id, ct);
        if (members.Count != memberIds.Length) return "One or more selected members are unavailable.";
        foreach (var input in request.Entries)
        {
            if (input.Lines.Count == 0 || input.Lines.Any(x => x.Amount <= 0)) return "Every collection entry requires at least one positive contribution amount.";
            var member = input.MemberId.HasValue ? members[input.MemberId.Value] : null;
            var entry = new ServiceCollectionEntry { Id = Guid.NewGuid(), ServiceCollectionId = item.Id, PartyType = member == null ? input.PartyType.Trim().ToUpperInvariant() : "MEMBER",
                MemberId = member?.Id, MembershipNumberSnapshot = member?.MembershipNumber, ContributorNameSnapshot = member == null ? Clean(input.ContributorName) ?? "Anonymous" : member.PreferredName ?? $"{member.FirstName} {member.LastName}".Trim(),
                ContributorReference = Clean(input.ContributorReference), Notes = Clean(input.Notes), CreatedById = actor, UpdatedById = actor };
            foreach (var line in input.Lines) entry.Lines.Add(new ServiceCollectionEntryLine { Id = Guid.NewGuid(), ServiceCollectionEntryId = entry.Id,
                ContributionTypeId = line.ContributionTypeId, Amount = Money(line.Amount), Notes = Clean(line.Notes), CreatedById = actor, UpdatedById = actor });
            item.Entries.Add(entry);
        }
        var fcy = await db.FcyManagementSettings.AsNoTracking().SingleAsync(ct);
        foreach (var input in request.Denominations)
        {
            if (input.Denomination <= 0 || input.Quantity <= 0) return "Denomination and quantity values must be positive.";
            var conversion = await Conversion(input.CurrencyCode, input.Denomination * input.Quantity, input.ExchangeRate, input.ExchangeRateId, input.IsManualRateOverride, input.OverrideReason, fcy, ct);
            if (conversion.Error != null) return conversion.Error;
            item.Denominations.Add(new ServiceCollectionDenomination { Id = Guid.NewGuid(), ServiceCollectionId = item.Id, CurrencyCode = conversion.Code,
                Denomination = input.Denomination, Quantity = input.Quantity, OriginalAmount = input.Denomination * input.Quantity, ExchangeRate = conversion.Rate,
                BaseEquivalent = conversion.Equivalent, ExchangeRateId = input.ExchangeRateId, IsManualRateOverride = input.IsManualRateOverride,
                OverrideReason = Clean(input.OverrideReason), CreatedById = actor, UpdatedById = actor });
        }
        var methodIds = request.Tenders.Select(x => x.PaymentMethodDefinitionId).Distinct().ToArray();
        var methods = await db.PaymentMethodDefinitions.AsNoTracking().Where(x => methodIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
        if (methods.Count != methodIds.Length) return "One or more payment methods are inactive or missing.";
        foreach (var input in request.Tenders)
        {
            if (input.OriginalAmount <= 0) return "Tender amount must be greater than zero.";
            var method = methods[input.PaymentMethodDefinitionId];
            if (method.RequiresReference && string.IsNullOrWhiteSpace(input.Reference)) return $"{method.Name} requires a reference.";
            if (method.RequiresBankAccount && !input.BankAccountId.HasValue) return $"{method.Name} requires a bank account.";
            if (!method.SupportsForeignCurrency && !input.CurrencyCode.Equals(fcy.BaseCurrencyCode, StringComparison.OrdinalIgnoreCase)) return $"{method.Name} does not support foreign currency.";
            if (method.Category == "CASH" && request.Denominations.Any(x => x.CurrencyCode.Equals(input.CurrencyCode, StringComparison.OrdinalIgnoreCase))) return "Do not capture the same cash in both denominations and a cash tender.";
            var conversion = await Conversion(input.CurrencyCode, input.OriginalAmount, input.ExchangeRate, input.ExchangeRateId, input.IsManualRateOverride, input.OverrideReason, fcy, ct);
            if (conversion.Error != null) return conversion.Error;
            item.Tenders.Add(new ServiceCollectionTender { Id = Guid.NewGuid(), ServiceCollectionId = item.Id, PaymentMethodDefinitionId = input.PaymentMethodDefinitionId,
                CurrencyCode = conversion.Code, OriginalAmount = input.OriginalAmount, ExchangeRate = conversion.Rate, BaseEquivalent = conversion.Equivalent,
                ExchangeRateId = input.ExchangeRateId, IsManualRateOverride = input.IsManualRateOverride, OverrideReason = Clean(input.OverrideReason),
                Reference = Clean(input.Reference), BankAccountId = input.BankAccountId, CreatedById = actor, UpdatedById = actor });
        }
        if (!await db.Users.AnyAsync(x => x.Id == actor && x.Status == "ACTIVE", ct)) return "Your active user profile could not be loaded.";
        item.Participants.Add(new ServiceCollectionParticipant { Id = Guid.NewGuid(), ServiceCollectionId = item.Id, UserId = actor,
            Responsibility = "COUNTER", CreatedById = actor, UpdatedById = actor });
        return null;
    }

    private async Task DeleteCapture(Guid collectionId, CancellationToken ct)
    {
        var entryIds = db.ServiceCollectionEntries.Where(x => x.ServiceCollectionId == collectionId).Select(x => x.Id);
        await db.ServiceCollectionEntryLines.Where(x => entryIds.Contains(x.ServiceCollectionEntryId)).ExecuteDeleteAsync(ct);
        await db.ServiceCollectionEntries.Where(x => x.ServiceCollectionId == collectionId).ExecuteDeleteAsync(ct);
        await db.ServiceCollectionDenominations.Where(x => x.ServiceCollectionId == collectionId).ExecuteDeleteAsync(ct);
        await db.ServiceCollectionTenders.Where(x => x.ServiceCollectionId == collectionId).ExecuteDeleteAsync(ct);
        await db.ServiceCollectionParticipants.Where(x => x.ServiceCollectionId == collectionId).ExecuteDeleteAsync(ct);
    }

    private async Task<string?> ValidateForSubmission(ServiceCollection item, CancellationToken ct)
    {
        if (item.Entries.Count == 0 || item.Entries.SelectMany(x => x.Lines).Count() == 0) return "Capture at least one contribution entry.";
        if (item.Denominations.Count == 0 && item.Tenders.Count == 0) return "Capture at least one denomination or payment tender.";
        if (item.Participants.Count == 0) return "Assign at least one counting participant.";
        if (item.Tenders.Any(x => x.PaymentMethodDefinition?.RequiresAttachment == true) &&
            !await db.ServiceCollectionAttachments.AsNoTracking().AnyAsync(x => x.EntityId == item.Id && x.DocumentType == "PAYMENT_PROOF", ct))
            return "Attach payment proof before submitting a POS/card or other payment method that requires evidence.";
        if (item.Variance != 0 && string.IsNullOrWhiteSpace(item.VarianceExplanation)) return "Explain the collection variance before submission.";
        var fcy = await db.FcyManagementSettings.AsNoTracking().SingleAsync(ct);
        if (!fcy.IsEnabled && item.Denominations.Any(x => x.CurrencyCode != fcy.BaseCurrencyCode) || !fcy.IsEnabled && item.Tenders.Any(x => x.CurrencyCode != fcy.BaseCurrencyCode)) return "FCY management is disabled but this collection contains foreign currency.";
        return null;
    }

    private async Task<(string Code, decimal Rate, decimal Equivalent, string? Error)> Conversion(string currencyCode, decimal amount, decimal requestedRate,
        Guid? rateId, bool manual, string? reason, FcyManagementSetting settings, CancellationToken ct)
    {
        var code = currencyCode.Trim().ToUpperInvariant(); if (!await db.Currencies.AnyAsync(x => x.Code == code && x.IsActive, ct)) return (code, 0, 0, "Select an active currency.");
        if (code == settings.BaseCurrencyCode) return (code, 1m, Money(amount), manual ? "The base currency cannot use a manual exchange-rate override." : null);
        if (!settings.IsEnabled) return (code, 0, 0, "FCY management is disabled in System Settings.");
        if (requestedRate <= 0) return (code, 0, 0, "A positive exchange rate is required for foreign currency.");
        if (manual && !settings.AllowManualRateOverride) return (code, 0, 0, "Manual rate overrides are disabled in System Settings.");
        if (manual && string.IsNullOrWhiteSpace(reason)) return (code, 0, 0, "A reason is required for a manual exchange-rate override.");
        if (!manual)
        {
            if (!rateId.HasValue) return (code, 0, 0, "Select an approved exchange rate or mark the rate as a manual override.");
            var maintained = await db.ExchangeRates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == rateId && x.IsApproved && x.FromCurrencyCode == code && x.ToCurrencyCode == settings.BaseCurrencyCode && x.EffectiveAtUtc <= DateTime.UtcNow, ct);
            if (maintained == null || maintained.Rate != requestedRate) return (code, 0, 0, "The selected exchange rate is invalid or has changed.");
            var tooOld = DateTime.UtcNow - maintained.EffectiveAtUtc > TimeSpan.FromHours(settings.MaximumRateAgeHours) || maintained.ExpiresAtUtc <= DateTime.UtcNow;
            if (tooOld && !settings.AllowLastKnownRateFallback) return (code, 0, 0, "The selected exchange rate is older than the FCY policy permits. Synchronize or maintain a current rate.");
        }
        else
        {
            var reference = await db.ExchangeRates.AsNoTracking().Where(x => x.IsApproved && x.FromCurrencyCode == code && x.ToCurrencyCode == settings.BaseCurrencyCode && x.EffectiveAtUtc <= DateTime.UtcNow)
                .OrderByDescending(x => x.EffectiveAtUtc).Select(x => (decimal?)x.Rate).FirstOrDefaultAsync(ct);
            if (reference.HasValue)
            {
                var variance = Math.Abs(requestedRate - reference.Value) / reference.Value * 100m;
                if (variance > settings.OverrideTolerancePercent && !settings.RequireApprovalForManualOverride)
                    return (code, 0, 0, $"The manual rate differs from the maintained rate by {variance:N2}%, beyond the allowed {settings.OverrideTolerancePercent:N2}% tolerance.");
            }
        }
        return (code, requestedRate, Money(amount * requestedRate), null);
    }

    private async Task<string?> ValidateHeader(SaveServiceCollectionRequest request, Guid? collectionId, CancellationToken ct)
    {
        var existingEventId = collectionId.HasValue
            ? await db.ServiceCollections.AsNoTracking().Where(x => x.Id == collectionId.Value).Select(x => (Guid?)x.ServiceEventId).FirstOrDefaultAsync(ct)
            : null;
        if (!await db.ServiceEvents.AnyAsync(x => x.Id == request.ServiceEventId &&
            (x.Status == ServiceEventStatuses.Active || existingEventId == x.Id), ct)) return "Select an active service event.";
        if (!await db.ContributionSources.AnyAsync(x => x.Id == request.ContributionSourceId && x.IsActive, ct)) return "Select an active contribution source.";
        return await ValidateScope(request.SectionId, request.GroupId, request.CommitteeId, ct);
    }

    private async Task<string?> ValidateScope(Guid? sectionId, Guid? groupId, Guid? committeeId, CancellationToken ct)
    {
        if (new[] { sectionId, groupId, committeeId }.Count(x => x.HasValue) > 1) return "Select at most one organizational scope.";
        if (sectionId.HasValue && !await db.Sections.AnyAsync(x => x.Id == sectionId && x.IsActive, ct)) return "Select an active section.";
        if (groupId.HasValue && !await db.Groups.AnyAsync(x => x.Id == groupId && x.IsActive, ct)) return "Select an active group.";
        if (committeeId.HasValue && !await db.Committees.AnyAsync(x => x.Id == committeeId && x.IsActive, ct)) return "Select an active committee.";
        if (!access.HasGlobalAccess() && !await access.CanAccessEntityAsync(groupId, sectionId, committeeId)) return "You are not assigned to the selected organizational scope.";
        return null;
    }

    private async Task DemandAccess(ServiceCollection item, CancellationToken ct)
    {
        if (access.HasGlobalAccess() || item.CreatedById == UserId()) return;
        if (item.Status == ServiceCollectionStatuses.Submitted && await IsStewardReviewer(ct)) return;
        if (!await access.CanAccessEntityAsync(item.GroupId, item.SectionId, item.CommitteeId)) throw new UnauthorizedAccessException("You cannot access this Service Collection.");
    }
    private async Task<bool> IsStewardReviewer(CancellationToken ct)
    {
        var actor = UserId();
        var now = DateTime.UtcNow;
        return await db.OfficeAppointments.AsNoTracking().AnyAsync(x => x.UserId == actor && x.IsActive &&
            x.EffectiveFromUtc <= now && (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc > now) &&
            x.User.Status == "ACTIVE" && x.Designation.IsActive && x.FunctionalArea.IsActive &&
            x.Designation.Code == DesignationCodes.Steward && x.User.Role == x.Designation.RequiredRole, ct);
    }
    private Task<bool> CanReview(ServiceCollection item, string capability, CancellationToken ct) => workflow.CanAsync(UserId(), capability,
        new AuthorizationResource(item.SectionId, item.GroupId, item.CommitteeId, FunctionalAreaCode: "STEWARDSHIP"), ct);
    private async Task ReleaseLocks(Guid id, Guid actor, CancellationToken ct) { foreach (var item in await db.ServiceCollectionLocks.Where(x => x.EntityId == id && x.ReleasedAtUtc == null).ToListAsync(ct)) { item.ReleasedAtUtc = DateTime.UtcNow; item.ReleasedById = actor; item.UpdatedById = actor; item.UpdatedDate = DateTime.UtcNow; item.Version++; } }
    private static void Recalculate(ServiceCollection item) { item.PurposeTotal = Money(item.Entries.SelectMany(x => x.Lines).Sum(x => x.Amount)); item.TenderTotal = Money(item.Denominations.Sum(x => x.BaseEquivalent) + item.Tenders.Sum(x => x.BaseEquivalent)); item.Variance = Money(item.TenderTotal - item.PurposeTotal); }
    private static IQueryable<ServiceCollection> Full(IQueryable<ServiceCollection> query) => query.Include(x => x.ServiceEvent).ThenInclude(x => x!.ServiceType).Include(x => x.ContributionSource)
        .Include(x => x.Section).Include(x => x.Group).Include(x => x.Committee)
        .Include(x => x.Entries).ThenInclude(x => x.Lines).ThenInclude(x => x.ContributionType).Include(x => x.Entries).ThenInclude(x => x.Member)
        .Include(x => x.Denominations).Include(x => x.Tenders).ThenInclude(x => x.PaymentMethodDefinition).Include(x => x.Participants).ThenInclude(x => x.User);
    private async Task<object> Reload(Guid id, CancellationToken ct)
    {
        var item = await Full(db.ServiceCollections.AsNoTracking()).FirstAsync(x => x.Id == id, ct);
        return Present(item, await UserName(item.CreatedById, ct));
    }
    private async Task<string?> UserName(Guid? id, CancellationToken ct) => id.HasValue
        ? await db.Users.AsNoTracking().Where(x => x.Id == id).Select(x => x.FirstName + " " + x.LastName).FirstOrDefaultAsync(ct)
        : null;
    private static object Present(ServiceCollection x, string? createdBy) => new { x.Id, x.CollectionNumber, x.ServiceEventId, x.ContributionSourceId, x.SectionId, x.GroupId, x.CommitteeId,
        x.CreatedById, CreatedBy = createdBy, x.CreatedDate, x.UpdatedDate,
        x.BaseCurrencyCode, x.Status, x.PurposeTotal, x.TenderTotal, x.Variance, x.Notes, x.VarianceExplanation, x.SubmittedAtUtc, x.SubmittedById,
        x.ApprovedAtUtc, x.ApprovedById, x.ReturnedAtUtc, x.ReturnedById, x.ReviewComments, x.ArchivedAtUtc, x.ArchiveReason, x.Version,
        ContributionSource = x.ContributionSource!.Name,
        Scope = x.Section != null ? x.Section.SectionName : x.Group != null ? x.Group.GroupName : x.Committee != null ? x.Committee.CommitteeName : "Organization-wide",
        ServiceEvent = new { x.ServiceEvent!.Id, x.ServiceEvent.ServiceTypeId, x.ServiceEvent.StartsAtUtc, x.ServiceEvent.SequenceName,
            ServiceType = x.ServiceEvent.ServiceType!.Name, x.ServiceEvent.Location, x.ServiceEvent.Attendance,
            x.ServiceEvent.ChildrenAttendance, x.ServiceEvent.TeacherAttendance, x.ServiceEvent.OfficiatingMinister,
            x.ServiceEvent.Status, x.ServiceEvent.Version },
        Entries = x.Entries.Select(e => new { e.Id, e.PartyType, e.MemberId, e.MembershipNumberSnapshot, e.ContributorNameSnapshot, e.ContributorReference, e.Notes,
            Lines = e.Lines.Select(l => new { l.Id, l.ContributionTypeId, ContributionType = l.ContributionType!.Name, l.Amount, l.Notes, l.ContributionId }) }),
        x.Denominations, Tenders = x.Tenders.Select(t => new { t.Id, t.PaymentMethodDefinitionId, PaymentMethod = t.PaymentMethodDefinition!.Name, t.CurrencyCode, t.OriginalAmount, t.ExchangeRate, t.BaseEquivalent, t.ExchangeRateId, t.IsManualRateOverride, t.OverrideReason, t.Reference, t.BankAccountId }),
        Participants = x.Participants.Select(p => new { p.Id, p.UserId, Name = p.User!.FirstName + " " + p.User.LastName, p.Responsibility,
            HasSignature = p.SignatureData != null && p.SignatureData != "", p.SignatureData, p.CompletedAtUtc }) };
    private static object CaptureAudit(ServiceCollection x) => new { x.ServiceEventId, x.ContributionSourceId, x.SectionId, x.GroupId, x.CommitteeId, x.PurposeTotal, x.TenderTotal, x.Variance,
        Entries = x.Entries.Select(e => new { e.PartyType, e.MemberId, e.MembershipNumberSnapshot, e.ContributorNameSnapshot, Lines = e.Lines.Select(l => new { l.ContributionTypeId, l.Amount }) }),
        Denominations = x.Denominations.Select(d => new { d.CurrencyCode, d.Denomination, d.Quantity, d.ExchangeRate, d.BaseEquivalent, d.IsManualRateOverride }),
        Tenders = x.Tenders.Select(t => new { t.PaymentMethodDefinitionId, t.CurrencyCode, t.OriginalAmount, t.ExchangeRate, t.ExchangeRateId, t.BaseEquivalent, t.Reference, t.IsManualRateOverride, t.OverrideReason }), x.Version };
    private void AddAudit(Guid id, string action, object changes, Guid actor) => db.ServiceCollectionAudits.Add(new ServiceCollectionAud
        { ServiceCollectionId = id, Action = action, ChangesJson = JsonSerializer.Serialize(changes), PerformedById = actor, CorrelationId = HttpContext.TraceIdentifier });
    private static string? ValidatePaymentMethod(SavePaymentMethodDefinitionRequest x) { if (string.IsNullOrWhiteSpace(x.Code) || x.Code.Trim().Length > 30) return "Code is required and cannot exceed 30 characters."; if (string.IsNullOrWhiteSpace(x.Name)) return "Name is required."; if (x.Category.Trim().ToUpperInvariant() is not ("CASH" or "BANK" or "DIGITAL" or "MOBILE" or "CARD" or "CHEQUE" or "OTHER")) return "Unknown payment category."; return null; }
    private static string? ValidateDateRange(DateTime? from, DateTime? to) =>
        from.HasValue && to.HasValue && from.Value.Date > to.Value.Date
            ? "The start date must be on or before the end date."
            : null;
    private DateTime LocalDateStartUtc(DateTime value)
    {
        var id = configuration["Platform:TimeZone"] ?? "UTC";
        TimeZoneInfo timeZone;
        try { timeZone = TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { throw new InvalidOperationException($"Configured platform time zone '{id}' is not installed on this host."); }
        catch (InvalidTimeZoneException) { throw new InvalidOperationException($"Configured platform time zone '{id}' is invalid."); }
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value.Date, DateTimeKind.Unspecified), timeZone);
    }
    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
