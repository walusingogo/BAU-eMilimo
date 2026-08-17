// backend/UCP.API/Services/ContributionService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using UCP.Core.DTOs;
using UCP.Core.Entities;
using UCP.Core.Entities.Companions;
using UCP.Core.Enums;
using UCP.Core.Services;
using UCP.Infrastructure.Data;
using UCP.API.Platform;

namespace UCP.API.Services
{
    public class ContributionService : IContributionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ContributionService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IGroupAccessService _groupAccess;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly ISystemLogWriter _logWriter;
        private readonly IApprovalNotificationService _approvalNotifications;
        private readonly IFileStorage _fileStorage;

        public ContributionService(
            AppDbContext context,
            ILogger<ContributionService> logger,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IGroupAccessService groupAccess,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            ISystemLogWriter logWriter,
            IApprovalNotificationService approvalNotifications,
            IFileStorage fileStorage)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _groupAccess = groupAccess;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _logWriter = logWriter;
            _approvalNotifications = approvalNotifications;
            _fileStorage = fileStorage;
        }

        /// <summary>
        /// Sends the contribution-status-changed notification to the creator
        /// and writes a user-friendly process log entry. Every caller of this
        /// method has already moved the contribution to a non-DRAFT status, so
        /// this never fires while a contribution remains in DRAFT. Never
        /// throws: a notification/logging failure must not undo or fail the
        /// business transaction that already committed.
        /// </summary>
        private async Task NotifyContributionStatusChangedAsync(Contribution contribution, string actorName, string? note = null)
        {
            try
            {
                if (contribution.Status == ContributionStatus.PENDING.ToString())
                {
                    await _approvalNotifications.NotifyAsync("contributions.confirm", "Contribution",
                        contribution.ContributionNumber, $"ZMW {contribution.Amount:N2} requires confirmation.",
                        $"contributions/{contribution.Id}", contribution.CreatedById,
                        allowedRoles: [nameof(UserRole.Accountant), nameof(UserRole.Treasurer)]);
                }

                if (!contribution.CreatedById.HasValue)
                    return;

                var creator = await _context.Users.FindAsync(contribution.CreatedById.Value);
                if (creator == null || string.IsNullOrWhiteSpace(creator.Email))
                    return;

                await _emailService.SendContributionStatusChangedAsync(
                    creator.Email,
                    $"{creator.FirstName} {creator.LastName}",
                    contribution.ContributionNumber,
                    contribution.Status,
                    note);

                if (contribution.Status == ContributionStatus.CONFIRMED.ToString() && contribution.UpdatedById.HasValue && contribution.UpdatedById != contribution.CreatedById)
                {
                    var approver = await _context.Users.FindAsync(contribution.UpdatedById.Value);
                    if (approver != null && !string.IsNullOrWhiteSpace(approver.Email))
                        await _emailService.SendContributionStatusChangedAsync(
                            approver.Email,
                            $"{approver.FirstName} {approver.LastName}".Trim(),
                            contribution.ContributionNumber,
                            contribution.Status,
                            "Your approval was recorded successfully.");
                }

                await _logWriter.WriteAsync(new SystemLog
                {
                    Level = "Information",
                    Category = "Process",
                    Process = "Contributions",
                    Action = "StatusChange",
                    UserId = contribution.UpdatedById,
                    Message = $"Contribution {contribution.ContributionNumber} status changed to {contribution.Status} by {actorName}."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contribution status-change notification for {ContributionId}", contribution.Id);
            }
        }

        private async Task<bool> CanReviewContributionAsync(Guid userId, Contribution contribution)
        {
            var user = await _context.Users.AsNoTracking().Where(x => x.Id == userId)
                .Select(x => new { x.Role, x.GroupId, x.SectionId }).FirstOrDefaultAsync();
            if (user == null) return false;
            if (user.Role == UserRole.SuperAdmin.ToString()) return true;
            return user.Role is nameof(UserRole.Accountant) or nameof(UserRole.Treasurer);
        }

        // ============================================
        // FILE STORAGE HELPERS
        // ============================================

        private string GetFileStorageBasePath() =>
            _configuration["FileStorage:BasePath"] ?? "uploads";

        private string GetContributionSubfolder() =>
            _configuration["FileStorage:ContributionSubfolder"] ?? "contributions";

        private string GetRelativeFolderPath(Guid contributionId)
        {
            var basePath = GetFileStorageBasePath().TrimStart('/').TrimEnd('/');
            var subFolder = GetContributionSubfolder().TrimStart('/').TrimEnd('/');
            return $"/{basePath}/{subFolder}/{contributionId}";
        }

        private string GetPhysicalPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return string.Empty;
            var trimmed = relativePath.TrimStart('/');
            return _fileStorage.GetPhysicalPath(trimmed);
        }

        // ============================================
        // STATS
        // ============================================

        public async Task<ContributionStatsDto> GetStatsAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new ContributionStatsDto();

            var query = _context.Contributions.AsQueryable();

            // Roles that have global access
            var globalRoles = new[]
            {
                UserRole.SuperAdmin.ToString(),
                UserRole.Admin.ToString(),
                UserRole.Accountant.ToString(),
                UserRole.Secretary.ToString(),
                UserRole.ChurchAuth.ToString(),
                UserRole.Minister.ToString(),
                UserRole.Auditor.ToString()
            };
            var hasGlobalAccess = _groupAccess.HasGlobalAccess() || globalRoles.Contains(user.Role);

            if (!hasGlobalAccess)
            {
                var accessibleIds = await _groupAccess.GetAccessibleGroupIdsAsync();
                var sections = await _groupAccess.GetAccessibleSectionIdsAsync();
                var committees = await _groupAccess.GetAccessibleCommitteeIdsAsync();
                query = query.Where(c => c.Allocations.Any(a =>
                    (a.GroupId.HasValue && accessibleIds.Contains(a.GroupId.Value)) ||
                    (a.SectionId.HasValue && sections.Contains(a.SectionId.Value)) ||
                    (a.CommitteeId.HasValue && committees.Contains(a.CommitteeId.Value))));
            }

            var all = await query.ToListAsync();

            var stats = new ContributionStatsDto
            {
                Total = all.Count,
                Draft = all.Count(c => c.Status == ContributionStatus.DRAFT.ToString()),
                Pending = all.Count(c => c.Status == ContributionStatus.PENDING.ToString()),
                Received = all.Count(c => c.Status == ContributionStatus.RECEIVED.ToString()),
                Confirmed = all.Count(c => c.Status == ContributionStatus.CONFIRMED.ToString()),
                Allocated = all.Count(c => c.Status == ContributionStatus.ALLOCATED.ToString()),
                Rejected = all.Count(c => c.Status == ContributionStatus.REJECTED.ToString()),
                Archived = all.Count(c => c.IsArchived),
                TotalAmount = all.Sum(c => c.Amount),
                PendingAmount = all.Where(c => c.Status == ContributionStatus.PENDING.ToString() || c.Status == ContributionStatus.RECEIVED.ToString())
                                .Sum(c => c.Amount),
                ConfirmedAmount = all.Where(c => c.Status == ContributionStatus.CONFIRMED.ToString() || c.Status == ContributionStatus.ALLOCATED.ToString())
                                .Sum(c => c.Amount)
            };

            return stats;
        }

        // ============================================
        // CREATE
        // ============================================

        public async Task<ContributionResponse> CreateContributionAsync(CreateContributionRequest request, Guid userId)
        {
            var contributionAmount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
            var totalAllocated = decimal.Round(
                request.Allocations.Sum(a => decimal.Round(a.AllocatedAmount, 2, MidpointRounding.AwayFromZero)),
                2,
                MidpointRounding.AwayFromZero);
            if (totalAllocated != contributionAmount)
            {
                throw new InvalidOperationException(
                    $"Total allocated amount ({totalAllocated:N2}) must equal contribution amount ({contributionAmount:N2}).");
            }

            foreach (var alloc in request.Allocations)
            {
                if (new[] { alloc.GroupId, alloc.SectionId, alloc.CommitteeId }.Count(x => x.HasValue) != 1)
                    throw new InvalidOperationException("Each allocation must select exactly one contributing entity.");
                if (!await _groupAccess.CanAccessEntityAsync(alloc.GroupId, alloc.SectionId, alloc.CommitteeId))
                    throw new UnauthorizedAccessException("You are not assigned to one or more selected entities.");
            }

            var contributionType = await _context.ContributionTypes.FindAsync(request.ContributionTypeId);
            if (contributionType == null)
                throw new KeyNotFoundException($"Contribution type with ID {request.ContributionTypeId} not found");

            var source = await _context.ContributionSources.FindAsync(request.SourceId);
            if (source == null)
                throw new KeyNotFoundException($"Source with ID {request.SourceId} not found");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var numberingLock = $"contribution-number-{DateTime.UtcNow.Year}";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({numberingLock}))");
            var contributionNumber = await GenerateContributionNumber();

            var contribution = new Contribution
            {
                Id = Guid.NewGuid(),
                ContributionNumber = contributionNumber,
                ContributionTypeId = request.ContributionTypeId,
                SourceId = request.SourceId,
                Amount = request.Amount,
                ContributionDate = request.ContributionDate,
                Description = request.Description,
                ReferenceNumber = request.ReferenceNumber,
                PaymentMethod = request.PaymentMethod,
                BankAccountId = request.BankAccountId,
                IsSplit = request.IsSplit || request.Allocations.Count > 1,
                Status = request.Status.ToString(),
                ReceivedBy = userId,
                CreatedById = userId,
                CreatedDate = DateTime.UtcNow,
                Version = 1
            };

            try
            {
                _context.Contributions.Add(contribution);
                await _context.SaveChangesAsync();

                foreach (var alloc in request.Allocations)
                {
                    var exists = alloc.GroupId.HasValue ? await _context.Groups.AnyAsync(x => x.Id == alloc.GroupId && x.IsActive) :
                        alloc.SectionId.HasValue ? await _context.Sections.AnyAsync(x => x.Id == alloc.SectionId && x.IsActive) :
                        await _context.Committees.AnyAsync(x => x.Id == alloc.CommitteeId && x.IsActive);
                    if (!exists) throw new KeyNotFoundException("Contributing entity not found or inactive.");

                    var allocation = new ContributionAllocation
                    {
                        Id = Guid.NewGuid(),
                        ContributionId = contribution.Id,
                        GroupId = alloc.GroupId,
                        SectionId = alloc.SectionId,
                        CommitteeId = alloc.CommitteeId,
                        AllocatedAmount = alloc.AllocatedAmount,
                        PercentageAllocation = alloc.PercentageAllocation ?? (alloc.AllocatedAmount / request.Amount * 100),
                        IsPrimary = alloc.IsPrimary,
                        Notes = alloc.Notes,
                        CreatedById = userId,
                        CreatedDate = DateTime.UtcNow,
                        Version = 1
                    };
                    _context.ContributionAllocations.Add(allocation);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (request.Files != null && request.Files.Any())
                {
                    await UploadAttachmentsAsync(contribution.Id, request.Files, userId);
                }

                if (contribution.Status == ContributionStatus.PENDING.ToString())
                {
                    var submitter = await _context.Users.FindAsync(userId);
                    await NotifyContributionStatusChangedAsync(contribution,
                        submitter != null ? $"{submitter.FirstName} {submitter.LastName}" : "the creator",
                        "This contribution has been submitted for confirmation.");
                }

                return await GetContributionByIdAsync(contribution.Id)
                    ?? throw new Exception("Failed to retrieve created contribution");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ============================================
        // CONFIRM
        // ============================================

        public async Task<ContributionResponse> ConfirmContributionAsync(Guid contributionId, Guid userId)
        {
            var contribution = await _context.Contributions
                .Include(c => c.ContributionType)
                .Include(c => c.Source)
                .Include(c => c.Allocations)
                    .ThenInclude(a => a.Group)
                .Include(c => c.Allocations).ThenInclude(a => a.Section)
                .Include(c => c.Allocations).ThenInclude(a => a.Committee)
                .Include(c => c.ReceivedByUser)
                .Include(c => c.ConfirmedByUser)
                .FirstOrDefaultAsync(c => c.Id == contributionId);

            if (contribution == null)
                throw new KeyNotFoundException("Contribution not found");

            if (!await CanReviewContributionAsync(userId, contribution))
                throw new UnauthorizedAccessException("Only an Accountant or Treasurer can confirm this contribution.");

            if (contribution.Status != ContributionStatus.PENDING.ToString() &&
                contribution.Status != ContributionStatus.RECEIVED.ToString())
            {
                throw new InvalidOperationException("Contribution cannot be confirmed");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                contribution.Status = ContributionStatus.CONFIRMED.ToString();
                contribution.ConfirmedBy = userId;
                contribution.ConfirmedAt = DateTime.UtcNow;
                contribution.UpdatedById = userId;
                contribution.UpdatedDate = DateTime.UtcNow;
                contribution.Version++;

                _context.Contributions.Update(contribution);
                await _context.SaveChangesAsync();

                foreach (var alloc in contribution.Allocations)
                {
                    var groupBalance = await _context.FundBalances.FirstOrDefaultAsync(fb => fb.FiscalYear == DateTime.UtcNow.Year &&
                        ((alloc.GroupId.HasValue && fb.GroupId == alloc.GroupId) || (alloc.SectionId.HasValue && fb.SectionId == alloc.SectionId) ||
                         (alloc.CommitteeId.HasValue && fb.CommitteeId == alloc.CommitteeId)));

                    if (groupBalance == null)
                    {
                        groupBalance = new FundBalance
                        {
                            Id = Guid.NewGuid(),
                            GroupId = alloc.GroupId,
                            SectionId = alloc.SectionId,
                            CommitteeId = alloc.CommitteeId,
                            FiscalYear = DateTime.UtcNow.Year,
                            BudgetedBalance = 0,
                            OwnFundsBalance = 0,
                            CommittedBudgetedBalance = 0,
                            CommittedOwnFundsBalance = 0,
                            CreatedDate = DateTime.UtcNow,
                            UpdatedDate = DateTime.UtcNow,
                            CreatedById = userId,
                            Version = 1
                        };
                        _context.FundBalances.Add(groupBalance);
                        await _context.SaveChangesAsync();
                    }

                    var previousBalance = groupBalance.OwnFundsBalance;
                    var newBalance = previousBalance + alloc.AllocatedAmount;

                    groupBalance.OwnFundsBalance = newBalance;
                    groupBalance.UpdatedDate = DateTime.UtcNow;
                    groupBalance.UpdatedById = userId;
                    groupBalance.Version++;

                    _context.FundBalances.Update(groupBalance);
                    await _context.SaveChangesAsync();

                    var metadata = new
                    {
                        contributionNumber = contribution.ContributionNumber,
                        allocatedAmount = alloc.AllocatedAmount
                    };

                    var transactionEntry = new FundTransaction
                    {
                        Id = Guid.NewGuid(),
                        GroupId = alloc.GroupId,
                        SectionId = alloc.SectionId,
                        CommitteeId = alloc.CommitteeId,
                        TransactionType = "ADD",
                        FundType = "OWN_FUNDS",
                        Amount = alloc.AllocatedAmount,
                        PreviousBalance = previousBalance,
                        NewBalance = newBalance,
                        Description = $"Contribution: {contribution.ContributionNumber}",
                        Source = contribution.Source?.Name ?? "Contribution",
                        ReferenceType = "CONTRIBUTION",
                        ReferenceId = contribution.Id,
                        Metadata = JsonSerializer.Serialize(metadata),
                        CreatedById = userId,
                        CreatedDate = DateTime.UtcNow,
                        Version = 1
                    };

                    _context.FundTransactions.Add(transactionEntry);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var confirmer = await _context.Users.FindAsync(userId);
                await NotifyContributionStatusChangedAsync(
                    contribution,
                    confirmer != null ? $"{confirmer.FirstName} {confirmer.LastName}" : "an administrator");

                return await GetContributionByIdAsync(contributionId)
                    ?? throw new Exception("Failed to retrieve confirmed contribution");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ============================================
        // REJECT
        // ============================================

        public async Task<ContributionResponse> RejectContributionAsync(Guid contributionId, RejectContributionDto dto, Guid userId)
        {
            var contribution = await _context.Contributions
                .Include(c => c.Allocations)
                .FirstOrDefaultAsync(c => c.Id == contributionId);

            if (contribution == null)
                throw new KeyNotFoundException("Contribution not found");

            if (!await CanReviewContributionAsync(userId, contribution))
                throw new UnauthorizedAccessException("Only an Accountant or Treasurer can reject this contribution.");

            if (contribution.Status != ContributionStatus.PENDING.ToString())
                throw new InvalidOperationException("Only pending contributions can be rejected");

            contribution.Status = ContributionStatus.REJECTED.ToString();
            contribution.RejectionReason = dto.Reason;
            contribution.UpdatedById = userId;
            contribution.UpdatedDate = DateTime.UtcNow;
            contribution.Version++;

            _context.Contributions.Update(contribution);
            await _context.SaveChangesAsync();

            var rejector = await _context.Users.FindAsync(userId);
            await NotifyContributionStatusChangedAsync(
                contribution,
                rejector != null ? $"{rejector.FirstName} {rejector.LastName}" : "an administrator",
                dto.Reason);

            return await GetContributionByIdAsync(contributionId)
                ?? throw new Exception("Failed to retrieve rejected contribution");
        }

        // ============================================
        // RETURN FOR CORRECTIONS
        // ============================================

        public async Task<ContributionResponse> ReturnContributionAsync(Guid contributionId, RejectContributionDto dto, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new InvalidOperationException("A correction reason is required.");

            var contribution = await _context.Contributions
                .Include(c => c.Allocations)
                .FirstOrDefaultAsync(c => c.Id == contributionId && !c.IsArchived);

            if (contribution == null)
                throw new KeyNotFoundException("Contribution not found");
            if (!await CanReviewContributionAsync(userId, contribution))
                throw new UnauthorizedAccessException("Only an Accountant or Treasurer can return this contribution for corrections.");
            if (contribution.Status != ContributionStatus.PENDING.ToString())
                throw new InvalidOperationException("Only pending contributions can be returned for corrections.");

            contribution.Status = ContributionStatus.DRAFT.ToString();
            contribution.RejectionReason = dto.Reason.Trim();
            contribution.ReceivedBy = null;
            contribution.ConfirmedBy = null;
            contribution.ConfirmedAt = null;
            contribution.UpdatedById = userId;
            contribution.UpdatedDate = DateTime.UtcNow;
            contribution.Version++;
            await _context.SaveChangesAsync();

            var reviewer = await _context.Users.FindAsync(userId);
            await NotifyContributionStatusChangedAsync(
                contribution,
                reviewer != null ? $"{reviewer.FirstName} {reviewer.LastName}" : "an approver",
                dto.Reason.Trim());

            return await GetContributionByIdAsync(contributionId)
                ?? throw new Exception("Failed to retrieve returned contribution");
        }

        // ============================================
        // SUBMIT
        // ============================================

        public async Task<ContributionResponse> SubmitContributionAsync(Guid contributionId, Guid userId)
        {
            var contribution = await _context.Contributions
                .Include(c => c.Allocations)
                .FirstOrDefaultAsync(c => c.Id == contributionId);

            if (contribution == null)
                throw new KeyNotFoundException("Contribution not found");

            // DRAFT contributions are private: only the creator may submit their
            // own draft, regardless of role.
            if (contribution.CreatedById != userId)
                throw new KeyNotFoundException("Contribution not found");

            if (contribution.Status != ContributionStatus.DRAFT.ToString())
                throw new InvalidOperationException("Only draft contributions can be submitted");

            contribution.Status = ContributionStatus.PENDING.ToString();
            contribution.UpdatedById = userId;
            contribution.UpdatedDate = DateTime.UtcNow;
            contribution.Version++;

            await _context.SaveChangesAsync();

            var submitter = await _context.Users.FindAsync(userId);
            await NotifyContributionStatusChangedAsync(
                contribution,
                submitter != null ? $"{submitter.FirstName} {submitter.LastName}" : "the creator",
                "Your contribution has been submitted for confirmation.");

            return await GetContributionByIdAsync(contributionId)
                ?? throw new Exception("Failed to retrieve submitted contribution");
        }

        // ============================================
        // UPDATE
        // ============================================

        public async Task<ContributionResponse> UpdateContributionAsync(Guid contributionId, UpdateContributionDto dto, Guid userId)
        {
            var contribution = await _context.Contributions
                .FirstOrDefaultAsync(c => c.Id == contributionId && !c.IsArchived);

            if (contribution == null)
                throw new KeyNotFoundException("Contribution not found");

            // DRAFT contributions are private working records: only the creator
            // may edit their own draft, regardless of role (including Admin/
            // SuperAdmin). Return NotFound rather than Unauthorized so another
            // user's draft isn't confirmed to exist. This must be checked before
            // anything else in the method.
            if (contribution.CreatedById != userId)
                throw new KeyNotFoundException("Contribution not found");

            if (contribution.Status != ContributionStatus.DRAFT.ToString())
                throw new InvalidOperationException("Only draft contributions can be updated");

            if (dto.Version.HasValue && dto.Version.Value != contribution.Version)
            {
                throw new DbUpdateConcurrencyException(
                    $"This contribution has been modified by another user. Current version: {contribution.Version}, Your version: {dto.Version.Value}"
                );
            }

            var effectiveAmount = dto.Amount ?? contribution.Amount;

            if (effectiveAmount <= 0)
                throw new InvalidOperationException("Contribution amount must be greater than zero");

            if (dto.ContributionTypeId.HasValue)
            {
                var contributionType = await _context.ContributionTypes
                    .FirstOrDefaultAsync(x => x.Id == dto.ContributionTypeId.Value && x.IsActive);

                if (contributionType == null)
                    throw new KeyNotFoundException($"Active contribution type with ID {dto.ContributionTypeId.Value} not found");

                contribution.ContributionTypeId = dto.ContributionTypeId.Value;
            }

            if (dto.SourceId.HasValue)
            {
                var source = await _context.ContributionSources
                    .FirstOrDefaultAsync(x => x.Id == dto.SourceId.Value && x.IsActive);

                if (source == null)
                    throw new KeyNotFoundException($"Active contribution source with ID {dto.SourceId.Value} not found");

                contribution.SourceId = dto.SourceId.Value;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (dto.Allocations != null)
                {
                    if (dto.Allocations.Count == 0)
                        throw new InvalidOperationException("At least one allocation is required");

                    var totalAllocated = dto.Allocations.Sum(a => a.AllocatedAmount);
                    if (Math.Abs(totalAllocated - effectiveAmount) > 0.01m)
                    {
                        throw new InvalidOperationException(
                            $"Total allocated amount ({totalAllocated:F2}) must equal contribution amount ({effectiveAmount:F2})"
                        );
                    }

                    var primaryCount = dto.Allocations.Count(a => a.IsPrimary);
                    if (primaryCount != 1)
                        throw new InvalidOperationException("Exactly one allocation must be marked as primary");

                    foreach (var allocation in dto.Allocations)
                    {
                        var selectedTargetCount = new[]
                        {
                            allocation.GroupId,
                            allocation.SectionId,
                            allocation.CommitteeId
                        }.Count(x => x.HasValue);

                        if (selectedTargetCount != 1)
                            throw new InvalidOperationException("Each allocation must select exactly one group, section, or committee");

                        if (allocation.AllocatedAmount <= 0)
                            throw new InvalidOperationException("Each allocation amount must be greater than zero");

                        if (!await _groupAccess.CanAccessEntityAsync(
                                allocation.GroupId,
                                allocation.SectionId,
                                allocation.CommitteeId))
                        {
                            throw new UnauthorizedAccessException(
                                "You are not assigned to one or more selected entities"
                            );
                        }

                        var entityExists = allocation.GroupId.HasValue
                            ? await _context.Groups.AnyAsync(x =>
                                x.Id == allocation.GroupId.Value && x.IsActive)
                            : allocation.SectionId.HasValue
                                ? await _context.Sections.AnyAsync(x =>
                                    x.Id == allocation.SectionId.Value && x.IsActive)
                                : await _context.Committees.AnyAsync(x =>
                                    x.Id == allocation.CommitteeId!.Value && x.IsActive);

                        if (!entityExists)
                            throw new KeyNotFoundException("One or more selected entities were not found or are inactive");
                    }

                    contribution.IsSplit = dto.Allocations.Count > 1;
                }
                else if (dto.IsSplit.HasValue)
                {
                    contribution.IsSplit = dto.IsSplit.Value;
                }

                if (dto.Amount.HasValue)
                    contribution.Amount = dto.Amount.Value;

                if (dto.ContributionDate.HasValue)
                    contribution.ContributionDate = NormalizeUtc(dto.ContributionDate.Value);

                if (dto.PaymentMethod != null)
                    contribution.PaymentMethod = dto.PaymentMethod;

                if (dto.Description != null)
                    contribution.Description = dto.Description;

                if (dto.ReferenceNumber != null)
                    contribution.ReferenceNumber = dto.ReferenceNumber;

                if (dto.BankAccountId.HasValue)
                {
                    var bankAccountExists = await _context.BankAccounts.AnyAsync(x =>
                        x.Id == dto.BankAccountId.Value && x.IsActive);

                    if (!bankAccountExists)
                        throw new KeyNotFoundException($"Active bank account with ID {dto.BankAccountId.Value} not found");

                    contribution.BankAccountId = dto.BankAccountId.Value;
                }
                else if (dto.PaymentMethod != null &&
                         dto.PaymentMethod.Equals("CASH", StringComparison.OrdinalIgnoreCase))
                {
                    contribution.BankAccountId = null;
                }

                contribution.UpdatedById = userId;
                contribution.UpdatedDate = DateTime.UtcNow;
                contribution.Version++;

                // Claim the parent contribution version before replacing its child
                // allocation rows. This keeps optimistic concurrency on the draft
                // itself from being invalidated by database-side allocation handling.
                await _context.SaveChangesAsync();

                if (dto.Allocations != null)
                {
                    await _context.ContributionAllocations
                        .Where(a => a.ContributionId == contribution.Id)
                        .ExecuteDeleteAsync();

                    var replacementAllocations = dto.Allocations
                        .Select(allocationDto => new ContributionAllocation
                        {
                            Id = Guid.NewGuid(),
                            ContributionId = contribution.Id,
                            GroupId = allocationDto.GroupId,
                            SectionId = allocationDto.SectionId,
                            CommitteeId = allocationDto.CommitteeId,
                            AllocatedAmount = allocationDto.AllocatedAmount,
                            PercentageAllocation = allocationDto.PercentageAllocation
                                ?? (allocationDto.AllocatedAmount / effectiveAmount * 100),
                            IsPrimary = allocationDto.IsPrimary,
                            Notes = allocationDto.Notes,
                            CreatedById = userId,
                            CreatedDate = DateTime.UtcNow,
                            Version = 1
                        })
                        .ToList();

                    _context.ContributionAllocations.AddRange(replacementAllocations);

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return await GetContributionByIdAsync(contributionId)
                ?? throw new InvalidOperationException("The contribution was updated but could not be reloaded");
        }

        // ============================================
        // ARCHIVE
        // ============================================

        public async Task ArchiveContributionAsync(Guid contributionId, ArchiveContributionDto dto, Guid userId)
        {
            var contribution = await _context.Contributions
                .FirstOrDefaultAsync(c => c.Id == contributionId);

            if (contribution == null)
                throw new KeyNotFoundException("Contribution not found");

            // DRAFT contributions are private: only the creator may archive/
            // delete their own draft.
            if (contribution.CreatedById != userId)
                throw new KeyNotFoundException("Contribution not found");

            if (contribution.Status != ContributionStatus.DRAFT.ToString())
                throw new InvalidOperationException("Only draft contributions can be archived");

            contribution.IsArchived = true;
            contribution.ArchivedAt = DateTime.UtcNow;
            contribution.ArchivedBy = userId;
            contribution.ArchiveReason = dto.Reason;
            contribution.UpdatedById = userId;
            contribution.UpdatedDate = DateTime.UtcNow;
            contribution.Version++;

            _context.Contributions.Update(contribution);
            await _context.SaveChangesAsync();
        }

        // ============================================
        // GET BY ID – MODIFIED for global roles
        // ============================================

        public async Task<ContributionResponse?> GetContributionByIdAsync(Guid id)
        {
            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                throw new UnauthorizedAccessException("User not authenticated");

            var user = await _context.Users.FindAsync(userId.Value);
            var userRole = user?.Role;

            // Roles that have global access
            var globalRoles = new[]
            {
                UserRole.SuperAdmin.ToString(),
                UserRole.Admin.ToString(),
                UserRole.Accountant.ToString(),
                UserRole.Secretary.ToString(),
                UserRole.ChurchAuth.ToString(),
                UserRole.Minister.ToString(),
                UserRole.Auditor.ToString()
            };
            var hasGlobalAccess = _groupAccess.HasGlobalAccess() || globalRoles.Contains(userRole);

            var accessibleIds = await _groupAccess.GetAccessibleGroupIdsAsync();
            var accessibleSections = await _groupAccess.GetAccessibleSectionIdsAsync();
            var accessibleCommittees = await _groupAccess.GetAccessibleCommitteeIdsAsync();

            var contribution = await _context.Contributions
                .Include(c => c.ContributionType)
                .Include(c => c.Source)
                .Include(c => c.Allocations)
                    .ThenInclude(a => a.Group)
                .Include(c => c.Allocations)
                    .ThenInclude(a => a.Section)
                .Include(c => c.Allocations)
                    .ThenInclude(a => a.Committee)
                .Include(c => c.ReceivedByUser)
                .Include(c => c.ConfirmedByUser)
                .Include(c => c.CreatedByUser)
                .Include(c => c.UpdatedByUser)
                .Include(c => c.Attachments)
                .Include(c => c.BankAccount)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsArchived &&
                    (
                        // DRAFT contributions are private working records: only
                        // the creator may see them, regardless of role or
                        // group/section/committee access. This check overrides
                        // hasGlobalAccess for DRAFT records specifically.
                        (c.Status == ContributionStatus.DRAFT.ToString() && c.CreatedById == userId.Value) ||
                        (c.Status != ContributionStatus.DRAFT.ToString() &&
                         (hasGlobalAccess ||
                          c.CreatedById == userId.Value ||
                          c.Allocations.Any(a =>
                              (a.GroupId.HasValue && accessibleIds.Contains(a.GroupId.Value)) ||
                              (a.SectionId.HasValue && accessibleSections.Contains(a.SectionId.Value)) ||
                              (a.CommitteeId.HasValue && accessibleCommittees.Contains(a.CommitteeId.Value))
                          )
                         )
                        )
                    )
                );

            // Return null (404 in the controller) rather than throwing Unauthorized
            // for a DRAFT owned by someone else, so the record's existence isn't
            // leaked to users who aren't its creator.
            return contribution != null ? MapToResponse(contribution) : null;
        }

        // ============================================
        // LIST – MODIFIED for global roles
        // ============================================

        public async Task<(List<ContributionResponse> Data, int Total)> GetContributionsAsync(ContributionFilter filter)
        {
            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                throw new UnauthorizedAccessException("User not authenticated");

            var user = await _context.Users.FindAsync(userId.Value);
            var userRole = user?.Role;

            // Roles that have global access
            var globalRoles = new[]
            {
                UserRole.SuperAdmin.ToString(),
                UserRole.Admin.ToString(),
                UserRole.Accountant.ToString(),
                UserRole.Secretary.ToString(),
                UserRole.ChurchAuth.ToString(),
                UserRole.Minister.ToString(),
                UserRole.Auditor.ToString()
            };
            var hasGlobalAccess = _groupAccess.HasGlobalAccess() || globalRoles.Contains(userRole);

            var accessibleIds = await _groupAccess.GetAccessibleGroupIdsAsync();
            var accessibleSections = await _groupAccess.GetAccessibleSectionIdsAsync();
            var accessibleCommittees = await _groupAccess.GetAccessibleCommitteeIdsAsync();

            var query = _context.Contributions
                .Include(c => c.ContributionType)
                .Include(c => c.Source)
                .Include(c => c.Allocations)
                    .ThenInclude(a => a.Group)
                .Include(c => c.Allocations).ThenInclude(a => a.Section)
                .Include(c => c.Allocations).ThenInclude(a => a.Committee)
                .Include(c => c.ReceivedByUser)
                .Include(c => c.ConfirmedByUser)
                .Where(c => !c.IsArchived);

            // Global roles bypass entity access
            if (!hasGlobalAccess)
            {
                query = query.Where(c =>
                    c.CreatedById == userId.Value ||
                    c.Allocations.Any(a =>
                        (a.GroupId.HasValue && accessibleIds.Contains(a.GroupId.Value)) ||
                        (a.SectionId.HasValue && accessibleSections.Contains(a.SectionId.Value)) ||
                        (a.CommitteeId.HasValue && accessibleCommittees.Contains(a.CommitteeId.Value))
                    )
                );
            }

            // DRAFT contributions are private: hide anyone else's drafts from
            // lists/search entirely, even for globally-accessible roles.
            query = query.Where(c =>
                c.Status != ContributionStatus.DRAFT.ToString() || c.CreatedById == userId.Value);

            // Apply additional filters
            if (filter.StartDate.HasValue)
            {
                var startDateUtc = NormalizeUtc(filter.StartDate.Value);
                query = query.Where(c => c.ContributionDate >= startDateUtc);
            }

            if (filter.EndDate.HasValue)
            {
                var endDateUtc = NormalizeUtc(filter.EndDate.Value);
                query = query.Where(c => c.ContributionDate <= endDateUtc);
            }

            if (filter.TypeId.HasValue)
                query = query.Where(c => c.ContributionTypeId == filter.TypeId.Value);

            if (filter.SourceId.HasValue)
                query = query.Where(c => c.SourceId == filter.SourceId.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(c => c.Status == filter.Status);

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var searchTerm = filter.Search.ToLower();
                query = query.Where(c =>
                    c.ContributionNumber.ToLower().Contains(searchTerm) ||
                    (c.Description != null && c.Description.ToLower().Contains(searchTerm)) ||
                    (c.ReferenceNumber != null && c.ReferenceNumber.ToLower().Contains(searchTerm))
                );
            }

            if (filter.GroupId.HasValue)
                query = query.Where(c => c.Allocations.Any(a => a.GroupId == filter.GroupId.Value));

            if (filter.SectionId.HasValue)
                query = query.Where(c => c.Allocations.Any(a => a.SectionId == filter.SectionId.Value));

            if (filter.CommitteeId.HasValue)
                query = query.Where(c => c.Allocations.Any(a => a.CommitteeId == filter.CommitteeId.Value));

            var total = await query.CountAsync();

            var data = await query
                .OrderBy(c => c.Description ?? string.Empty)
                .ThenBy(c => c.ContributionNumber)
                .Skip(filter.Offset)
                .Take(filter.Limit)
                .Select(c => MapToResponse(c))
                .ToListAsync();

            return (data, total);
        }

        private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        // ============================================
        // DASHBOARD STATS
        // ============================================

        public async Task<ContributionDashboardStats> GetDashboardStatsAsync(string period)
        {
            var userId = _currentUserService.GetUserId();
            var user = userId.HasValue ? await _context.Users.FindAsync(userId.Value) : null;
            var userRole = user?.Role;

            // Roles that have global access
            var globalRoles = new[]
            {
                UserRole.SuperAdmin.ToString(),
                UserRole.Admin.ToString(),
                UserRole.Accountant.ToString(),
                UserRole.Secretary.ToString(),
                UserRole.ChurchAuth.ToString(),
                UserRole.Minister.ToString(),
                UserRole.Auditor.ToString()
            };
            var hasGlobalAccess = _groupAccess.HasGlobalAccess() || globalRoles.Contains(userRole);

            var accessibleIds = await _groupAccess.GetAccessibleGroupIdsAsync();
            var now = DateTime.UtcNow;
            var startDate = period.ToLower() switch
            {
                "month" => now.AddMonths(-1),
                "quarter" => now.AddMonths(-3),
                "year" => now.AddYears(-1),
                _ => now.AddMonths(-1)
            };

            var previousStart = startDate - (now - startDate);

            var currentSummary = await _context.Contributions
                .Where(c => c.Status == ContributionStatus.CONFIRMED.ToString() &&
                            c.ContributionDate >= startDate &&
                            !c.IsArchived &&
                            (hasGlobalAccess || c.Allocations.Any(a => a.GroupId.HasValue && accessibleIds.Contains(a.GroupId.Value))))
                .GroupBy(c => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    TotalAmount = g.Sum(c => c.Amount),
                    AverageAmount = g.Average(c => c.Amount)
                })
                .FirstOrDefaultAsync();

            var previousTotal = await _context.Contributions
                .Where(c => c.Status == ContributionStatus.CONFIRMED.ToString() &&
                            c.ContributionDate >= previousStart &&
                            c.ContributionDate < startDate &&
                            !c.IsArchived &&
                            (hasGlobalAccess || c.Allocations.Any(a => a.GroupId.HasValue && accessibleIds.Contains(a.GroupId.Value))))
                .SumAsync(c => c.Amount);

            var stats = new ContributionDashboardStats
            {
                Summary = new ContributionSummary
                {
                    TotalContributions = currentSummary?.Count ?? 0,
                    TotalAmount = currentSummary?.TotalAmount ?? 0,
                    AverageAmount = currentSummary?.AverageAmount ?? 0,
                    PreviousPeriodTotal = previousTotal,
                    PercentageChange = previousTotal > 0
                        ? (double)((currentSummary?.TotalAmount ?? 0 - previousTotal) / previousTotal * 100)
                        : 0
                },
                ByType = new List<ContributionByTypeStats>(),
                BySource = new List<ContributionBySourceStats>(),
                ByGroup = new List<ContributionByGroupStats>(),
                Recent = new List<RecentContributionResponse>()
            };

            return stats;
        }

        // ============================================
        // DROPDOWN DATA
        // ============================================

        public async Task<List<ContributionTypeResponse>> GetContributionTypesAsync()
        {
            return await _context.ContributionTypes
                .OrderBy(t => t.DisplayOrder)
                .Select(t => new ContributionTypeResponse
                {
                    Id = t.Id,
                    Code = t.Code,
                    Name = t.Name,
                    Description = t.Description,
                    IsActive = t.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<ContributionSourceResponse>> GetContributionSourcesAsync()
        {
            return await _context.ContributionSources
                .OrderBy(s => s.Name)
                .Select(s => new ContributionSourceResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<BankAccountResponse>> GetBankAccountsAsync()
        {
            return await _context.BankAccounts
                .Where(b => b.IsActive)
                .OrderBy(b => b.BankName)
                .Select(b => new BankAccountResponse
                {
                    Id = b.Id,
                    BankName = b.BankName,
                    AccountName = b.AccountName,
                    AccountNumber = b.AccountNumber,
                    Branch = b.Branch,
                    AccountType = b.AccountType
                })
                .ToListAsync();
        }

        // ============================================
        // ATTACHMENT METHODS
        // ============================================

        public async Task<List<ContributionAttachmentResponse>> UploadAttachmentsAsync(Guid contributionId, List<IFormFile> files, Guid userId)
        {
            var contribution = await _context.Contributions
                .FirstOrDefaultAsync(c => c.Id == contributionId && !c.IsArchived);

            if (contribution == null)
                throw new KeyNotFoundException("Contribution not found");

            var relativeFolder = GetRelativeFolderPath(contributionId);
            var physicalFolder = GetPhysicalPath(relativeFolder);
            Directory.CreateDirectory(physicalFolder);

            var attachments = new List<ContributionAttachmentResponse>();

            foreach (var file in files)
            {
                if (file.Length == 0)
                    continue;

                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    throw new InvalidOperationException($"File type {extension} is not allowed. Allowed types: PDF, JPG, JPEG, PNG");

                if (file.Length > 10 * 1024 * 1024)
                    throw new InvalidOperationException("File size exceeds 10MB limit");

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(physicalFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = $"{relativeFolder}/{fileName}";

                var attachment = new ContributionAtm
                {
                    Id = Guid.NewGuid(),
                    EntityId = contributionId,
                    FileName = file.FileName,
                    FilePath = relativePath,
                    MimeType = file.ContentType,
                    Size = file.Length,
                    UploadedById = userId,
                    UploadedAt = DateTime.UtcNow,
                    Description = $"Uploaded by user {userId}",
                    CreatedById = userId,
                    CreatedDate = DateTime.UtcNow,
                    Version = 1
                };

                _context.ContributionAtms.Add(attachment);
                await _context.SaveChangesAsync();

                attachments.Add(new ContributionAttachmentResponse
                {
                    Id = attachment.Id,
                    FileName = attachment.FileName,
                    FilePath = attachment.FilePath ?? string.Empty,
                    Size = attachment.Size,
                    MimeType = attachment.MimeType,
                    UploadedAt = attachment.UploadedAt,
                    UploadedBy = attachment.UploadedById
                });
            }

            return attachments;
        }

        public async Task<List<ContributionAttachmentResponse>> GetAttachmentsAsync(Guid contributionId)
        {
            return await _context.ContributionAtms
                .Where(a => a.EntityId == contributionId)
                .OrderByDescending(a => a.UploadedAt)
                .Select(a => new ContributionAttachmentResponse
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath ?? string.Empty,
                    Size = a.Size,
                    MimeType = a.MimeType,
                    UploadedAt = a.UploadedAt,
                    UploadedBy = a.UploadedById
                })
                .ToListAsync();
        }

        public async Task DeleteAttachmentAsync(Guid contributionId, Guid attachmentId, Guid userId)
        {
            var attachment = await _context.ContributionAtms
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.EntityId == contributionId);

            if (attachment == null)
                throw new KeyNotFoundException("Attachment not found");

            var physicalPath = GetPhysicalPath(attachment.FilePath);
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            _context.ContributionAtms.Remove(attachment);
            await _context.SaveChangesAsync();
        }

        // ============================================
        // NEW: GET NEXT CONTRIBUTION NUMBER
        // ============================================

        public async Task<string> GetNextContributionNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var last = await _context.Contributions
                .Where(c => c.ContributionNumber.StartsWith($"CONT-{year}-"))
                .OrderByDescending(c => c.ContributionNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (last != null)
            {
                var parts = last.ContributionNumber.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }

            return $"CONT-{year}-{sequence:D6}";
        }

        // ============================================
        // PRIVATE HELPERS
        // ============================================

        private async Task<string> GenerateContributionNumber()
        {
            var year = DateTime.UtcNow.Year;
            var last = await _context.Contributions
                .Where(c => c.ContributionNumber.StartsWith($"CONT-{year}-"))
                .OrderByDescending(c => c.ContributionNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (last != null)
            {
                var parts = last.ContributionNumber.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }

            return $"CONT-{year}-{sequence:D6}";
        }

        private static ContributionResponse MapToResponse(Contribution c)
        {
            return new ContributionResponse
            {
                Id = c.Id,
                ContributionNumber = c.ContributionNumber,
                ContributionType = c.ContributionType != null ? new ContributionTypeResponse
                {
                    Id = c.ContributionType.Id,
                    Code = c.ContributionType.Code,
                    Name = c.ContributionType.Name,
                    Description = c.ContributionType.Description,
                    IsActive = c.ContributionType.IsActive
                } : null,
                Source = c.Source != null ? new ContributionSourceResponse
                {
                    Id = c.Source.Id,
                    Name = c.Source.Name,
                    Description = c.Source.Description,
                    IsActive = c.Source.IsActive
                } : null,
                Amount = c.Amount,
                ContributionDate = c.ContributionDate,
                Description = c.Description,
                ReferenceNumber = c.ReferenceNumber,
                PaymentMethod = c.PaymentMethod,
                BankAccount = c.BankAccount != null ? new BankAccountResponse
                {
                    Id = c.BankAccount.Id,
                    BankName = c.BankAccount.BankName,
                    AccountName = c.BankAccount.AccountName,
                    AccountNumber = c.BankAccount.AccountNumber,
                    Branch = c.BankAccount.Branch,
                    AccountType = c.BankAccount.AccountType
                } : null,
                RejectionReason = c.RejectionReason,
                ArchiveReason = c.ArchiveReason,
                Status = c.Status,
                IsSplit = c.IsSplit,
                Version = c.Version,
                Allocations = c.Allocations.Select(a => new ContributionAllocationResponse
                {
                    Id = a.Id,
                    Group = a.Group != null ? new GroupBriefResponse
                    {
                        Id = a.Group.Id,
                        GroupName = a.Group.GroupName,
                        GroupType = a.Group.GroupType
                    } : null,
                    Entity = a.Group != null ? new OrganizationBriefResponse { Id = a.Group.Id, Name = a.Group.GroupName, EntityType = "Group" } :
                        a.Section != null ? new OrganizationBriefResponse { Id = a.Section.Id, Name = a.Section.SectionName, EntityType = "Section" } :
                        a.Committee != null ? new OrganizationBriefResponse { Id = a.Committee.Id, Name = a.Committee.CommitteeName, EntityType = "Committee" } : null,
                    AllocatedAmount = a.AllocatedAmount,
                    PercentageAllocation = a.PercentageAllocation,
                    IsPrimary = a.IsPrimary,
                    Notes = a.Notes
                }).ToList(),
                ReceivedBy = c.ReceivedByUser != null ? new UserBriefResponse
                {
                    Id = c.ReceivedByUser.Id,
                    FirstName = c.ReceivedByUser.FirstName,
                    LastName = c.ReceivedByUser.LastName,
                    Email = c.ReceivedByUser.Email
                } : null,
                ConfirmedBy = c.ConfirmedByUser != null ? new UserBriefResponse
                {
                    Id = c.ConfirmedByUser.Id,
                    FirstName = c.ConfirmedByUser.FirstName,
                    LastName = c.ConfirmedByUser.LastName,
                    Email = c.ConfirmedByUser.Email,
                    SignatureData = c.ConfirmedByUser.SignatureData
                } : null,
                ConfirmedAt = c.ConfirmedAt,
                CreatedDate = c.CreatedDate,
                UpdatedDate = c.UpdatedDate,
                CreatedBy = c.CreatedByUser != null ? new UserBriefResponse
                {
                    Id = c.CreatedByUser.Id,
                    FirstName = c.CreatedByUser.FirstName,
                    LastName = c.CreatedByUser.LastName,
                    Email = c.CreatedByUser.Email,
                    SignatureData = c.CreatedByUser.SignatureData
                } : null,
                UpdatedBy = c.UpdatedByUser != null ? new UserBriefResponse
                {
                    Id = c.UpdatedByUser.Id,
                    FirstName = c.UpdatedByUser.FirstName,
                    LastName = c.UpdatedByUser.LastName,
                    Email = c.UpdatedByUser.Email
                } : null,
                Attachments = c.Attachments?.Select(a => new ContributionAttachmentResponse
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath ?? string.Empty,
                    Size = a.Size,
                    MimeType = a.MimeType,
                    UploadedAt = a.UploadedAt,
                    UploadedBy = a.UploadedById
                }).ToList() ?? new List<ContributionAttachmentResponse>()
            };
        }
    }
}
