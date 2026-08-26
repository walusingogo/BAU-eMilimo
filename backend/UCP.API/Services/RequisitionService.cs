// backend/UCP.API/Services/RequisitionService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UCP.Core.DTOs;
using UCP.Core.Entities;
using UCP.Core.Entities.Companions;
using UCP.Core.Enums;
using UCP.Core.Services;
using UCP.Core.Utils;
using UCP.Infrastructure.Data;
using UCP.API.Platform;

namespace UCP.API.Services
{
    public class RequisitionService : IRequisitionService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<RequisitionService> _logger;
        private readonly IGroupAccessService _groupAccess;
        private readonly IEmailService _emailService;
        private readonly ISystemLogWriter _logWriter;
        private readonly IApprovalNotificationService _approvalNotifications;
        private readonly IFileStorage _fileStorage;

        public RequisitionService(
            AppDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            ILogger<RequisitionService> logger,
            IGroupAccessService groupAccess,
            IEmailService emailService,
            ISystemLogWriter logWriter,
            IApprovalNotificationService approvalNotifications,
            IFileStorage fileStorage)
        {
            _emailService = emailService;
            _logWriter = logWriter;
            _context = context;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _groupAccess = groupAccess;
            _approvalNotifications = approvalNotifications;
            _fileStorage = fileStorage;
        }

        // ============================================
        // STATS
        // ============================================

        private static bool HasGlobalRequisitionReadAccess(string? role) => role is
            nameof(UserRole.SuperAdmin) or nameof(UserRole.Admin) or
            nameof(UserRole.ChurchAuth) or nameof(UserRole.Treasurer) or
            nameof(UserRole.Accountant) or nameof(UserRole.Minister);

        public async Task<RequisitionStatsDto> GetStatsAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new RequisitionStatsDto();

            var query = _context.Requisitions.AsQueryable();

            if (user.Role == UserRole.GroupRep.ToString())
            {
                var groups = await _groupAccess.GetAccessibleGroupIdsAsync();
                var sections = await _groupAccess.GetAccessibleSectionIdsAsync();
                var committees = await _groupAccess.GetAccessibleCommitteeIdsAsync();
                query = query.Where(r => r.RequisitorId == userId ||
                    (r.Status == RequisitionStatus.SENIOR_REPS_PENDING.ToString() &&
                     ((r.GroupId.HasValue && groups.Contains(r.GroupId.Value)) ||
                      (r.SectionId.HasValue && sections.Contains(r.SectionId.Value)) ||
                      (r.CommitteeId.HasValue && committees.Contains(r.CommitteeId.Value)))));
            }
            else if (!HasGlobalRequisitionReadAccess(user.Role))
            {
                var groups = await _groupAccess.GetAccessibleGroupIdsAsync();
                var sections = await _groupAccess.GetAccessibleSectionIdsAsync();
                var committees = await _groupAccess.GetAccessibleCommitteeIdsAsync();
                query = query.Where(r => (r.GroupId.HasValue && groups.Contains(r.GroupId.Value)) ||
                    (r.SectionId.HasValue && sections.Contains(r.SectionId.Value)) ||
                    (r.CommitteeId.HasValue && committees.Contains(r.CommitteeId.Value)));
            }

            query = query.Where(r => r.Status != RequisitionStatus.DRAFT.ToString() || r.RequisitorId == userId);

            var all = await query.ToListAsync();

            var stats = new RequisitionStatsDto
            {
                Total = all.Count,
                Draft = all.Count(r => r.Status == RequisitionStatus.DRAFT.ToString()),
                Pending = all.Count(r => r.Status == RequisitionStatus.SENIOR_REPS_PENDING.ToString() ||
                                        r.Status == RequisitionStatus.CHURCH_AUTH_PENDING.ToString() ||
                                        r.Status == RequisitionStatus.MINISTER_PENDING.ToString()),
                Approved = all.Count(r => r.Status == RequisitionStatus.APPROVED.ToString()),
                Rejected = all.Count(r => r.Status == RequisitionStatus.REJECTED.ToString()),
                MoneyReady = all.Count(r => r.Status == RequisitionStatus.MONEY_READY.ToString()),
                Collected = all.Count(r => r.Status == RequisitionStatus.COLLECTED.ToString()),
                Completed = all.Count(r => r.Status == RequisitionStatus.COMPLETED.ToString()),
                Archived = all.Count(r => r.IsArchived),
                TotalAmount = all.Sum(r => r.Amount),
                PendingAmount = all.Where(r => r.Status == RequisitionStatus.SENIOR_REPS_PENDING.ToString() ||
                                                r.Status == RequisitionStatus.CHURCH_AUTH_PENDING.ToString() ||
                                                r.Status == RequisitionStatus.MINISTER_PENDING.ToString())
                                .Sum(r => r.Amount),
                ApprovedAmount = all.Where(r => r.Status == RequisitionStatus.APPROVED.ToString() ||
                                                r.Status == RequisitionStatus.MONEY_READY.ToString() ||
                                                r.Status == RequisitionStatus.COLLECTED.ToString() ||
                                                r.Status == RequisitionStatus.COMPLETED.ToString())
                                .Sum(r => r.Amount)
            };

            return stats;
        }

        // ---------- Helper ----------
        private async Task<FundBalance?> GetCurrentBalance(Guid? groupId, Guid? sectionId, Guid? committeeId)
        {
            var fiscalYear = DateTime.UtcNow.Year;
            return await _context.FundBalances
                .FirstOrDefaultAsync(fb => fb.FiscalYear == fiscalYear &&
                    ((groupId.HasValue && fb.GroupId == groupId) || (sectionId.HasValue && fb.SectionId == sectionId) ||
                     (committeeId.HasValue && fb.CommitteeId == committeeId)));
        }

        // ---------- Helper: Get Available Balance ----------
        private async Task<(decimal availableBudgeted, decimal availableOwnFunds)> GetAvailableBalanceAsync(Guid? groupId, Guid? sectionId, Guid? committeeId)
        {
            var balance = await GetCurrentBalance(groupId, sectionId, committeeId);
            if (balance == null)
                return (0, 0);

            var availableBudgeted = balance.BudgetedBalance - balance.CommittedBudgetedBalance;
            var availableOwnFunds = balance.OwnFundsBalance - balance.CommittedOwnFundsBalance;
            return (availableBudgeted, availableOwnFunds);
        }

        // ============================================
        // CREATE
        // ============================================

        public async Task<Requisition> CreateRequisitionAsync(CreateRequisitionDto dto, Guid userId)
        {
            if (dto.Items == null || !dto.Items.Any())
                throw new InvalidOperationException("A requisition must contain at least one item.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found. Please log in again.");

            var entityCount = new[] { dto.GroupId, dto.SectionId, dto.CommitteeId }.Count(x => x.HasValue);
            if (entityCount != 1) throw new InvalidOperationException("Select exactly one requesting entity.");
            var entityExists = dto.GroupId.HasValue ? await _context.Groups.AnyAsync(x => x.Id == dto.GroupId && x.IsActive) :
                dto.SectionId.HasValue ? await _context.Sections.AnyAsync(x => x.Id == dto.SectionId && x.IsActive) :
                await _context.Committees.AnyAsync(x => x.Id == dto.CommitteeId && x.IsActive);
            if (!entityExists) throw new KeyNotFoundException("Requesting entity not found or inactive.");
            if (!await _groupAccess.CanAccessEntityAsync(dto.GroupId, dto.SectionId, dto.CommitteeId))
                throw new UnauthorizedAccessException("You are not assigned to this requesting entity.");

            var amount = dto.Items.Sum(item => item.UnitPrice * item.Quantity);

            var (availableBudgeted, availableOwnFunds) = await GetAvailableBalanceAsync(dto.GroupId, dto.SectionId, dto.CommitteeId);
            if (dto.FundSource == FundSource.Budgeted && amount > availableBudgeted)
                throw new InvalidOperationException($"Insufficient budget balance. Available: {availableBudgeted}, Requested: {amount}");
            else if (dto.FundSource == FundSource.OwnFunds && amount > availableOwnFunds)
                throw new InvalidOperationException($"Insufficient own funds balance. Available: {availableOwnFunds}, Requested: {amount}");

            var requisition = new Requisition
            {
                Id = Guid.NewGuid(),
                RequisitionNumber = $"REQ-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                GroupId = dto.GroupId,
                SectionId = dto.SectionId,
                CommitteeId = dto.CommitteeId,
                RequisitorId = userId,
                Payee = dto.Payee,
                PaymentMethod = dto.PaymentMethod.ToString(),
                FundSource = dto.FundSource.ToString(),
                Amount = amount,
                AmountWords = NumberToWords.Convert(amount),
                PaymentDetails = dto.PaymentDetails,
                Status = RequisitionStatus.DRAFT.ToString(),
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                CreatedById = userId,
                UpdatedById = userId,
                LastProcessedTimeStamp = DateTime.UtcNow,
                Version = 1,
                Items = dto.Items.Select(item => new RequisitionItem
                {
                    Id = Guid.NewGuid(),
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Total = item.UnitPrice * item.Quantity,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    Version = 1
                }).ToList()
            };

            try
            {
                _context.Requisitions.Add(requisition);
                await _context.SaveChangesAsync();

                if (dto.Files != null && dto.Files.Any())
                {
                    await UploadAttachmentsAsync(requisition.Id, dto.Files, userId);
                }

                return requisition;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving requisition. Inner exception: {InnerException}", ex.InnerException?.Message);
                throw new InvalidOperationException($"Failed to create requisition. Inner error: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
        }

        // ============================================
        // GET BY ID
        // ============================================

        public async Task<Requisition> GetRequisitionAsync(Guid id, Guid userId)
        {
            var requisition = await _context.Requisitions
                .Include(r => r.Items)
                .Include(r => r.Group)
                .Include(r => r.Section)
                .Include(r => r.Committee)
                .Include(r => r.Requisitor)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (requisition == null)
                throw new KeyNotFoundException("Requisition not found.");

            // DRAFT requisitions are private working records: only the creator
            // may see them, regardless of role. Return NotFound (not Unauthorized)
            // so the record's existence isn't leaked to other users. This check
            // takes priority over the normal role/entity-access checks below.
            if (requisition.Status == RequisitionStatus.DRAFT.ToString() && requisition.RequisitorId != userId)
                throw new KeyNotFoundException("Requisition not found.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            if (user.Role == UserRole.GroupRep.ToString() && requisition.RequisitorId != userId)
            {
                var canReviewEntityRequest = requisition.Status == RequisitionStatus.SENIOR_REPS_PENDING.ToString() &&
                    await BelongsToRequestingEntityAsync(user, requisition);
                if (!canReviewEntityRequest)
                    throw new KeyNotFoundException("Requisition not found.");
            }

            if (requisition.RequisitorId != userId &&
                user.Role != UserRole.GroupRep.ToString() &&
                !HasGlobalRequisitionReadAccess(user.Role))
            {
                if (!await _groupAccess.CanAccessEntityAsync(requisition.GroupId, requisition.SectionId, requisition.CommitteeId))
                    throw new UnauthorizedAccessException("Access denied.");
            }

            return requisition;
        }

        // ============================================
        // LIST
        // ============================================

        public async Task<IEnumerable<Requisition>> GetRequisitionsAsync(RequisitionFilterDto filter, Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Enumerable.Empty<Requisition>();

            var query = _context.Requisitions
                .Include(r => r.Items)
                .Include(r => r.Group)
                .Include(r => r.Section)
                .Include(r => r.Committee)
                .Include(r => r.Requisitor)
                .AsQueryable();

            if (user.Role == UserRole.GroupRep.ToString())
            {
                var groups = await _groupAccess.GetAccessibleGroupIdsAsync();
                var sections = await _groupAccess.GetAccessibleSectionIdsAsync();
                var committees = await _groupAccess.GetAccessibleCommitteeIdsAsync();
                query = query.Where(r => r.RequisitorId == userId ||
                    (r.Status == RequisitionStatus.SENIOR_REPS_PENDING.ToString() &&
                     ((r.GroupId.HasValue && groups.Contains(r.GroupId.Value)) ||
                      (r.SectionId.HasValue && sections.Contains(r.SectionId.Value)) ||
                      (r.CommitteeId.HasValue && committees.Contains(r.CommitteeId.Value)))));
            }
            else if (!HasGlobalRequisitionReadAccess(user.Role))
            {
                var groups = await _groupAccess.GetAccessibleGroupIdsAsync();
                var sections = await _groupAccess.GetAccessibleSectionIdsAsync();
                var committees = await _groupAccess.GetAccessibleCommitteeIdsAsync();
                query = query.Where(r => (r.GroupId.HasValue && groups.Contains(r.GroupId.Value)) ||
                    (r.SectionId.HasValue && sections.Contains(r.SectionId.Value)) ||
                    (r.CommitteeId.HasValue && committees.Contains(r.CommitteeId.Value)));
            }

            // DRAFT requisitions are private: hide anyone else's drafts from lists/
            // search entirely, even for SuperAdmin/Admin and even if the role or
            // group/section/committee access above would otherwise include them.
            query = query.Where(r =>
                r.Status != RequisitionStatus.DRAFT.ToString() || r.RequisitorId == userId);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(r => r.Status == filter.Status);

            if (filter.GroupId.HasValue)
                query = query.Where(r => r.GroupId == filter.GroupId.Value);
            if (filter.SectionId.HasValue)
                query = query.Where(r => r.SectionId == filter.SectionId.Value);
            if (filter.CommitteeId.HasValue)
                query = query.Where(r => r.CommitteeId == filter.CommitteeId.Value);

            if (filter.FromDate.HasValue)
            {
                var fromDateUtc = NormalizeUtc(filter.FromDate.Value);
                query = query.Where(r => r.CreatedDate >= fromDateUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toDateUtc = NormalizeUtc(filter.ToDate.Value);
                query = query.Where(r => r.CreatedDate <= toDateUtc);
            }

            return await query
                .OrderBy(r => r.Items!.Select(i => i.Description).Min() ?? string.Empty)
                .ThenBy(r => r.RequisitionNumber)
                .ToListAsync();
        }

        private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        // ============================================
        // UPDATE - FIXED CONCURRENCY
        // ============================================

        public async Task<Requisition> UpdateRequisitionAsync(Guid id, UpdateRequisitionDto dto, Guid userId)
        {
            var requisition = await _context.Requisitions
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsArchived);

            if (requisition == null)
                throw new KeyNotFoundException($"Requisition with ID {id} not found");

            // Older clients did not include Version in multipart updates. Use the
            // loaded row version in that case, while still rejecting a supplied
            // stale version.
            var clientVersion = dto.Version ?? requisition.Version;
            if (dto.Version.HasValue && requisition.Version != clientVersion)
            {
                throw new DbUpdateConcurrencyException(
                    $"The requisition has been modified since you loaded it. Current version: {requisition.Version}, your version: {clientVersion}");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            if (requisition.Status != RequisitionStatus.DRAFT.ToString())
                throw new InvalidOperationException("Only draft requisitions can be updated");

            // DRAFT requisitions are private working records: only the creator can
            // edit them while they remain in DRAFT. No role (including Admin/
            // SuperAdmin) may edit another user's draft — this is intentionally
            // stricter than the general "isAdmin" bypass used elsewhere, because
            // DRAFT visibility/ownership rules are absolute per the DRAFT-privacy
            // requirement. Once submitted, normal role-based authorization applies.
            if (requisition.RequisitorId != userId)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to update this requisition. Only the requisitor may edit a draft.");
            }

            // Apply changes
            if (!string.IsNullOrWhiteSpace(dto.Payee))
                requisition.Payee = dto.Payee;

            if (dto.PaymentMethod.HasValue)
                requisition.PaymentMethod = dto.PaymentMethod.Value.ToString();

            if (!string.IsNullOrWhiteSpace(dto.PaymentDetails))
                requisition.PaymentDetails = dto.PaymentDetails;

            if (dto.Amount.HasValue)
                requisition.Amount = dto.Amount.Value;

            if (dto.Items != null && dto.Items.Any())
            {
                requisition.Amount = dto.Items.Sum(i => i.Quantity * i.UnitPrice);
                requisition.AmountWords = NumberToWords.Convert(requisition.Amount);
            }

            // ---------- CRITICAL CONCURRENCY FIX ----------
            // Set the original value of Version to the client's version so EF uses it in the WHERE clause
            _context.Entry(requisition).Property(r => r.Version).OriginalValue = clientVersion;

            // Set the new version to client version + 1
            requisition.Version = clientVersion + 1;

            // Audit fields
            requisition.UpdatedDate = DateTime.UtcNow;
            requisition.UpdatedById = userId;
            requisition.LastProcessedTimeStamp = DateTime.UtcNow;

            try
            {
                // The requisition was loaded by this context and is already tracked.
                // Calling Update() here traverses newly-created items with assigned
                // GUIDs and incorrectly marks them as Modified instead of Added.
                // Claim the parent version before replacing its item rows.
                await _context.SaveChangesAsync();

                if (dto.Items != null && dto.Items.Any())
                {
                    await _context.RequisitionItems
                        .Where(item => item.RequisitionId == requisition.Id)
                        .ExecuteDeleteAsync();

                    var replacementItems = dto.Items.Select(item => new RequisitionItem
                    {
                        Id = Guid.NewGuid(),
                        RequisitionId = requisition.Id,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Total = item.Quantity * item.UnitPrice,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        CreatedById = userId,
                        Version = 1
                    });

                    _context.RequisitionItems.AddRange(replacementItems);
                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await _context.Entry(requisition).ReloadAsync();
                throw new DbUpdateConcurrencyException(
                    $"This requisition has been modified by another user. Current version: {requisition.Version}",
                    ex);
            }

            return await _context.Requisitions
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id) ?? requisition;
        }

        // ============================================
        // SUBMIT
        // ============================================

        public async Task<Requisition> SubmitRequisitionAsync(Guid id, Guid userId)
        {
            var requisition = await LoadOwnedRequisitionAsync(id, userId);

            if (requisition.Status != RequisitionStatus.DRAFT.ToString())
                throw new InvalidOperationException("Only draft requisitions may be submitted.");

            var balance = await GetCurrentBalance(requisition.GroupId, requisition.SectionId, requisition.CommitteeId);
            if (balance == null)
                throw new InvalidOperationException("No fund balance found for this group.");

            var fundSource = Enum.Parse<FundSource>(requisition.FundSource);
            decimal previousAvailable;
            decimal newAvailable;
            if (fundSource == FundSource.Budgeted)
            {
                var availableBudgeted = balance.BudgetedBalance - balance.CommittedBudgetedBalance;
                if (requisition.Amount > availableBudgeted)
                    throw new InvalidOperationException($"Insufficient budget balance. Available: {availableBudgeted}, Requested: {requisition.Amount}");
                previousAvailable = availableBudgeted;
                balance.CommittedBudgetedBalance += requisition.Amount;
                newAvailable = balance.BudgetedBalance - balance.CommittedBudgetedBalance;
            }
            else if (fundSource == FundSource.OwnFunds)
            {
                var availableOwnFunds = balance.OwnFundsBalance - balance.CommittedOwnFundsBalance;
                if (requisition.Amount > availableOwnFunds)
                    throw new InvalidOperationException($"Insufficient own funds balance. Available: {availableOwnFunds}, Requested: {requisition.Amount}");
                previousAvailable = availableOwnFunds;
                balance.CommittedOwnFundsBalance += requisition.Amount;
                newAvailable = balance.OwnFundsBalance - balance.CommittedOwnFundsBalance;
            }
            else throw new InvalidOperationException("Unsupported requisition fund source.");

            balance.UpdatedDate = DateTime.UtcNow;
            balance.Version++;
            _context.FundBalances.Update(balance);
            _context.FundTransactions.Add(new FundTransaction
            {
                Id = Guid.NewGuid(), GroupId = requisition.GroupId, SectionId = requisition.SectionId, CommitteeId = requisition.CommitteeId,
                TransactionType = "COMMITMENT", FundType = requisition.FundSource, Amount = -requisition.Amount,
                PreviousBalance = previousAvailable, NewBalance = newAvailable,
                Description = $"Funds reserved for requisition {requisition.RequisitionNumber}", Source = "REQUISITION",
                ReferenceType = "REQUISITION", ReferenceId = requisition.Id, CreatedById = userId
            });

            requisition.Status = RequisitionStatus.SENIOR_REPS_PENDING.ToString();
            requisition.UpdatedDate = DateTime.UtcNow;
            requisition.UpdatedById = userId;
            requisition.LastProcessedTimeStamp = DateTime.UtcNow;
            requisition.Version++;
            await _context.SaveChangesAsync();

            var submitter = await _context.Users.FindAsync(userId);
            await NotifyRequisitionStatusChangedAsync(
                requisition,
                submitter != null ? $"{submitter.FirstName} {submitter.LastName}" : "the requisitor",
                "Your requisition has been submitted for approval.");

            return requisition;
        }

        // ============================================
        // APPROVE
        // ============================================

        private int GetSeniorApprovalCount(Requisition requisition)
        {
            int count = 0;
            if (requisition.FirstSeniorApproverId.HasValue) count++;
            if (requisition.SecondSeniorApproverId.HasValue) count++;
            return count;
        }

        private bool HasUserApproved(Requisition requisition, Guid userId)
        {
            return requisition.FirstSeniorApproverId == userId || requisition.SecondSeniorApproverId == userId ||
                   requisition.TreasurerApproverId == userId || requisition.AccountantApproverId == userId ||
                   requisition.ChurchAuthorityApproverId == userId || requisition.MinisterApproverId == userId;
        }

        private async Task<bool> BelongsToRequestingEntityAsync(User user, Requisition requisition)
        {
            if (requisition.GroupId.HasValue)
                return user.GroupId == requisition.GroupId || await _context.UserGroupAssignments.AnyAsync(x => x.UserId == user.Id && x.GroupId == requisition.GroupId);
            if (requisition.SectionId.HasValue) return user.SectionId == requisition.SectionId;
            if (requisition.CommitteeId.HasValue)
                return await _context.UserCommitteeAssignments.AnyAsync(x => x.UserId == user.Id && x.CommitteeId == requisition.CommitteeId);
            return false;
        }

        private async Task<decimal> MinisterApprovalThresholdAsync()
        {
            var value = await _context.SystemConfigs.AsNoTracking().Where(x => x.ConfigKey == "minister_approval_threshold").Select(x => x.ConfigValue).FirstOrDefaultAsync();
            return decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var threshold) ? threshold : 0m;
        }

        public async Task<Requisition> ApproveRequisitionAsync(Guid id, ApproveRequisitionDto dto, Guid userId)
        {
            var requisition = await LoadRequisitionAsync(id);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            if (requisition.Status == RequisitionStatus.REJECTED.ToString() || requisition.Status == RequisitionStatus.ARCHIVED.ToString())
                throw new InvalidOperationException("Cannot approve a requisition in its current state.");

            // SuperAdmin retains the system-wide emergency override. Administrators
            // still manage the process, but do not replace its designated signers.
            if (user.Role == UserRole.SuperAdmin.ToString())
            {
                return await FinalApproveRequisitionAsync(requisition, userId);
            }

            var currentStatus = requisition.Status;

            if (currentStatus == RequisitionStatus.SENIOR_REPS_PENDING.ToString())
            {
                if (user.Role is not (nameof(UserRole.SeniorRep) or nameof(UserRole.GroupRep)))
                    throw new UnauthorizedAccessException("Only a Senior Representative or Group Representative can approve at this stage.");
                if (requisition.RequisitorId == userId)
                    throw new UnauthorizedAccessException("The requisitor cannot approve their own requisition.");
                if (!await BelongsToRequestingEntityAsync(user, requisition))
                    throw new UnauthorizedAccessException("The approver must belong to the same group, section, or committee as the requisitor.");

                if (HasUserApproved(requisition, userId))
                    throw new InvalidOperationException("You have already approved this requisition.");

                if (!requisition.FirstSeniorApproverId.HasValue)
                {
                    requisition.FirstSeniorApproverId = userId;
                }
                else if (!requisition.SecondSeniorApproverId.HasValue)
                {
                    if (user.Role == nameof(UserRole.GroupRep))
                    {
                        var firstApproverIsSenior = await _context.Users.AnyAsync(x =>
                            x.Id == requisition.FirstSeniorApproverId && x.Role == nameof(UserRole.SeniorRep));
                        if (!firstApproverIsSenior)
                            throw new InvalidOperationException("At least one of the two entity approvals must be made by a Senior Representative.");
                    }
                    requisition.SecondSeniorApproverId = userId;
                }
                else
                {
                    throw new InvalidOperationException("Both entity representative approval slots are already filled.");
                }

                var movedToChurchAuth = GetSeniorApprovalCount(requisition) >= 2;
                if (movedToChurchAuth)
                {
                    requisition.Status = RequisitionStatus.CHURCH_AUTH_PENDING.ToString();
                }

                requisition.UpdatedDate = DateTime.UtcNow;
                requisition.UpdatedById = userId;
                requisition.LastProcessedTimeStamp = DateTime.UtcNow;
                requisition.Version++;
                await _context.SaveChangesAsync();

                if (movedToChurchAuth)
                {
                    await NotifyRequisitionStatusChangedAsync(requisition, $"{user.FirstName} {user.LastName}");
                }

                return requisition;
            }

            if (currentStatus == RequisitionStatus.CHURCH_AUTH_PENDING.ToString())
            {
                if (HasUserApproved(requisition, userId)) throw new InvalidOperationException("You have already approved this requisition.");
                if (user.Role == UserRole.Treasurer.ToString()) requisition.TreasurerApproverId = userId;
                else if (user.Role == UserRole.Accountant.ToString()) requisition.AccountantApproverId = userId;
                else if (user.Role is nameof(UserRole.ChurchAuth) or nameof(UserRole.Secretary)) requisition.ChurchAuthorityApproverId = userId;
                else throw new UnauthorizedAccessException("This stage requires one Treasurer, one Accountant, and one Church Authority or Secretary.");

                var churchStageComplete = requisition.TreasurerApproverId.HasValue && requisition.AccountantApproverId.HasValue && requisition.ChurchAuthorityApproverId.HasValue;
                if (churchStageComplete)
                {
                    var threshold = await MinisterApprovalThresholdAsync();
                    if (requisition.Amount > threshold) requisition.Status = RequisitionStatus.MINISTER_PENDING.ToString();
                }
                requisition.UpdatedDate = DateTime.UtcNow;
                requisition.UpdatedById = userId;
                requisition.LastProcessedTimeStamp = DateTime.UtcNow;
                requisition.Version++;
                await _context.SaveChangesAsync();

                if (!churchStageComplete) return requisition;
                if (requisition.Status == RequisitionStatus.MINISTER_PENDING.ToString())
                { await NotifyRequisitionStatusChangedAsync(requisition, $"{user.FirstName} {user.LastName}"); return requisition; }
                return await FinalApproveRequisitionAsync(requisition, userId);
            }

            if (currentStatus == RequisitionStatus.MINISTER_PENDING.ToString())
            {
                if (user.Role != UserRole.Minister.ToString())
                    throw new UnauthorizedAccessException("Only Minister can approve at this stage.");
                requisition.MinisterApproverId = userId;
                return await FinalApproveRequisitionAsync(requisition, userId);
            }

            throw new InvalidOperationException("Requisition is not in a pending state.");
        }

        // ============================================
        // REJECT
        // ============================================

        public async Task<Requisition> RejectRequisitionAsync(Guid id, RejectRequisitionDto dto, Guid userId)
        {
            var requisition = await LoadRequisitionAsync(id);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            if (requisition.Status == RequisitionStatus.ARCHIVED.ToString())
                throw new InvalidOperationException("Cannot reject an archived requisition.");

            bool canReject = user.Role == UserRole.SuperAdmin.ToString();
            if (!canReject)
            {
                var currentStatus = requisition.Status;
                if (currentStatus == RequisitionStatus.SENIOR_REPS_PENDING.ToString() &&
                    (requisition.RequisitorId == userId ||
                     user.Role is not (nameof(UserRole.SeniorRep) or nameof(UserRole.GroupRep)) ||
                     !await BelongsToRequestingEntityAsync(user, requisition)))
                    throw new UnauthorizedAccessException("Only another Senior Representative or Group Representative from the same requesting entity may reject at this stage.");
                else if (currentStatus == RequisitionStatus.CHURCH_AUTH_PENDING.ToString() && user.Role is not (nameof(UserRole.Treasurer) or nameof(UserRole.Accountant) or nameof(UserRole.ChurchAuth) or nameof(UserRole.Secretary)))
                    throw new UnauthorizedAccessException("You are not authorized to reject at this stage.");
                else if (currentStatus == RequisitionStatus.MINISTER_PENDING.ToString() && user.Role != UserRole.Minister.ToString())
                    throw new UnauthorizedAccessException("You are not authorized to reject at this stage.");
                else if (!new[] { "SENIOR_REPS_PENDING", "CHURCH_AUTH_PENDING", "MINISTER_PENDING" }.Contains(currentStatus))
                    throw new InvalidOperationException("Requisition is not in a pending state.");
            }

            var balance = await GetCurrentBalance(requisition.GroupId, requisition.SectionId, requisition.CommitteeId);
            if (balance != null)
            {
                var fundSource = Enum.Parse<FundSource>(requisition.FundSource);
                if (fundSource == FundSource.Budgeted)
                    balance.CommittedBudgetedBalance -= requisition.Amount;
                else if (fundSource == FundSource.OwnFunds)
                    balance.CommittedOwnFundsBalance -= requisition.Amount;

                balance.UpdatedDate = DateTime.UtcNow;
                balance.Version++;
                _context.FundBalances.Update(balance);
            }

            requisition.Status = RequisitionStatus.REJECTED.ToString();
            requisition.RejectionReason = dto.Reason;
            requisition.UpdatedDate = DateTime.UtcNow;
            requisition.UpdatedById = userId;
            requisition.LastProcessedTimeStamp = DateTime.UtcNow;
            requisition.Version++;
            await _context.SaveChangesAsync();

            await NotifyRequisitionStatusChangedAsync(requisition, $"{user.FirstName} {user.LastName}", dto.Reason);

            return requisition;
        }

        // ============================================
        // RETURN FOR CORRECTIONS
        // ============================================

        public async Task<Requisition> ReturnRequisitionAsync(Guid id, RejectRequisitionDto dto, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new InvalidOperationException("A correction reason is required.");

            var requisition = await LoadRequisitionAsync(id);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var pendingStatuses = new[]
            {
                RequisitionStatus.SENIOR_REPS_PENDING.ToString(),
                RequisitionStatus.CHURCH_AUTH_PENDING.ToString(),
                RequisitionStatus.MINISTER_PENDING.ToString()
            };
            if (!pendingStatuses.Contains(requisition.Status))
                throw new InvalidOperationException("Only a pending requisition can be returned for corrections.");

            var canReturn = user.Role == UserRole.SuperAdmin.ToString();
            if (!canReturn && requisition.Status == RequisitionStatus.SENIOR_REPS_PENDING.ToString())
                canReturn = requisition.RequisitorId != userId &&
                    user.Role is nameof(UserRole.SeniorRep) or nameof(UserRole.GroupRep) &&
                    await BelongsToRequestingEntityAsync(user, requisition);
            else if (!canReturn && requisition.Status == RequisitionStatus.CHURCH_AUTH_PENDING.ToString())
                canReturn = user.Role is nameof(UserRole.Treasurer) or nameof(UserRole.Accountant) or nameof(UserRole.ChurchAuth) or nameof(UserRole.Secretary);
            else if (!canReturn && requisition.Status == RequisitionStatus.MINISTER_PENDING.ToString())
                canReturn = user.Role == UserRole.Minister.ToString();

            if (!canReturn)
                throw new UnauthorizedAccessException("You are not authorized to return this requisition at its current stage.");

            var balance = await GetCurrentBalance(requisition.GroupId, requisition.SectionId, requisition.CommitteeId);
            if (balance != null)
            {
                var fundSource = Enum.Parse<FundSource>(requisition.FundSource);
                if (fundSource == FundSource.Budgeted)
                    balance.CommittedBudgetedBalance = Math.Max(0, balance.CommittedBudgetedBalance - requisition.Amount);
                else if (fundSource == FundSource.OwnFunds)
                    balance.CommittedOwnFundsBalance = Math.Max(0, balance.CommittedOwnFundsBalance - requisition.Amount);
                balance.UpdatedDate = DateTime.UtcNow;
                balance.Version++;
            }

            requisition.Status = RequisitionStatus.DRAFT.ToString();
            requisition.RejectionReason = dto.Reason.Trim();
            requisition.FirstSeniorApproverId = null;
            requisition.SecondSeniorApproverId = null;
            requisition.TreasurerApproverId = null;
            requisition.AccountantApproverId = null;
            requisition.ChurchAuthorityApproverId = null;
            requisition.MinisterApproverId = null;
            requisition.ApprovedById = null;
            requisition.ApprovedAt = null;
            requisition.UpdatedDate = DateTime.UtcNow;
            requisition.UpdatedById = userId;
            requisition.LastProcessedTimeStamp = DateTime.UtcNow;
            requisition.Version++;
            await _context.SaveChangesAsync();

            await NotifyRequisitionStatusChangedAsync(requisition, $"{user.FirstName} {user.LastName}", dto.Reason.Trim());
            return requisition;
        }

        // ============================================
        // MONEY READY
        // ============================================

        public async Task<Requisition> MarkMoneyReadyAsync(Guid id, Guid userId)
        {
            var requisition = await LoadRequisitionAsync(id);

            if (requisition.Status != RequisitionStatus.APPROVED.ToString())
                throw new InvalidOperationException("Only approved requisitions can be marked as money ready.");

            requisition.Status = RequisitionStatus.MONEY_READY.ToString();
            requisition.MoneyReadyAt = DateTime.UtcNow;
            requisition.MoneyReadyById = userId;
            requisition.UpdatedDate = DateTime.UtcNow;
            requisition.UpdatedById = userId;
            requisition.LastProcessedTimeStamp = DateTime.UtcNow;
            requisition.Version++;
            await _context.SaveChangesAsync();

            var actor = await _context.Users.FindAsync(userId);
            await NotifyRequisitionStatusChangedAsync(
                requisition,
                actor != null ? $"{actor.FirstName} {actor.LastName}" : "an administrator");

            return requisition;
        }

        // ============================================
        // COLLECT
        // ============================================

        public async Task<Requisition> CollectRequisitionAsync(Guid id, CollectRequisitionDto dto, Guid userId)
        {
            var requisition = await LoadRequisitionAsync(id);

            if (requisition.Status != RequisitionStatus.MONEY_READY.ToString())
                throw new InvalidOperationException("Only money-ready requisitions can be collected.");

            requisition.Status = RequisitionStatus.COLLECTED.ToString();
            requisition.CollectedById = userId;
            requisition.CollectionSignature = dto.SignatureData;
            requisition.CollectedAt = DateTime.UtcNow;
            requisition.UpdatedDate = DateTime.UtcNow;
            requisition.UpdatedById = userId;
            requisition.LastProcessedTimeStamp = DateTime.UtcNow;
            requisition.Version++;
            await _context.SaveChangesAsync();

            var collector = await _context.Users.FindAsync(userId);
            await NotifyRequisitionStatusChangedAsync(
                requisition,
                collector != null ? $"{collector.FirstName} {collector.LastName}" : "an administrator");

            return requisition;
        }

        // ============================================
        // ARCHIVE
        // ============================================

        public async Task ArchiveRequisitionAsync(Guid id, ArchiveRequisitionDto dto, Guid userId)
        {
            var requisition = await LoadRequisitionAsync(id);

            if (requisition.Status == RequisitionStatus.ARCHIVED.ToString())
                throw new InvalidOperationException("Requisition is already archived.");

            var pendingStatuses = new[] { RequisitionStatus.SENIOR_REPS_PENDING.ToString(), RequisitionStatus.CHURCH_AUTH_PENDING.ToString(), RequisitionStatus.MINISTER_PENDING.ToString() };
            if (pendingStatuses.Contains(requisition.Status))
            {
                var balance = await GetCurrentBalance(requisition.GroupId, requisition.SectionId, requisition.CommitteeId);
                if (balance != null)
                {
                    var fundSource = Enum.Parse<FundSource>(requisition.FundSource);
                    if (fundSource == FundSource.Budgeted)
                        balance.CommittedBudgetedBalance -= requisition.Amount;
                    else if (fundSource == FundSource.OwnFunds)
                        balance.CommittedOwnFundsBalance -= requisition.Amount;

                    balance.UpdatedDate = DateTime.UtcNow;
                    balance.Version++;
                    _context.FundBalances.Update(balance);
                }
            }

            requisition.Status = RequisitionStatus.ARCHIVED.ToString();
            requisition.IsArchived = true;
            requisition.ArchivedById = userId;
            requisition.ArchivedAt = DateTime.UtcNow;
            requisition.ArchiveReason = dto.Reason;
            requisition.UpdatedDate = DateTime.UtcNow;
            requisition.UpdatedById = userId;
            requisition.LastProcessedTimeStamp = DateTime.UtcNow;
            requisition.Version++;
            await _context.SaveChangesAsync();
        }

        // ============================================
        // UTILITY
        // ============================================

        public async Task<bool> CanUserApproveAsync(Guid requisitionId, Guid userId)
        {
            var requisition = await _context.Requisitions.FindAsync(requisitionId);
            if (requisition == null)
                return false;

            return requisition.Status != RequisitionStatus.ARCHIVED.ToString() && requisition.Status != RequisitionStatus.REJECTED.ToString();
        }

        public Task<RequisitionStatus> GetNextStatusAsync(Guid requisitionId)
        {
            var requisition = _context.Requisitions.Find(requisitionId);
            if (requisition == null)
                throw new KeyNotFoundException("Requisition not found.");

            var currentStatus = Enum.TryParse<RequisitionStatus>(requisition.Status, true, out var status)
                ? status
                : RequisitionStatus.DRAFT;

            return Task.FromResult(currentStatus switch
            {
                RequisitionStatus.DRAFT => RequisitionStatus.SENIOR_REPS_PENDING,
                RequisitionStatus.SENIOR_REPS_PENDING => RequisitionStatus.CHURCH_AUTH_PENDING,
                RequisitionStatus.CHURCH_AUTH_PENDING => RequisitionStatus.MINISTER_PENDING,
                RequisitionStatus.MINISTER_PENDING => RequisitionStatus.APPROVED,
                RequisitionStatus.APPROVED => RequisitionStatus.MONEY_READY,
                RequisitionStatus.MONEY_READY => RequisitionStatus.COLLECTED,
                RequisitionStatus.COLLECTED => RequisitionStatus.COMPLETED,
                _ => currentStatus
            });
        }

        // ============================================
        // PRIVATE LOADERS
        // ============================================

        /// <summary>
        /// Sends the requisition-status-changed notification to the requisitor
        /// and writes a user-friendly process log entry. DRAFT is never a valid
        /// "newStatus" here because every method that changes status also moves
        /// the record out of DRAFT — so this never fires for records still in
        /// DRAFT, per the "no status-change emails while DRAFT" requirement.
        /// Never throws: notification/logging problems must not break the
        /// underlying business transaction that already committed.
        /// </summary>
        private async Task NotifyRequisitionStatusChangedAsync(Requisition requisition, string actorName, string? note = null)
        {
            try
            {
                if (requisition.Status.EndsWith("_PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    var ccRoles = new List<string>();
                    if (requisition.Amount > await MinisterApprovalThresholdAsync()) ccRoles.Add(nameof(UserRole.Minister));
                    var (allowedRoles, groupId, sectionId, committeeId) = requisition.Status switch
                    {
                        nameof(RequisitionStatus.SENIOR_REPS_PENDING) =>
                            (new[] { nameof(UserRole.SeniorRep), nameof(UserRole.GroupRep) }, requisition.GroupId, requisition.SectionId, requisition.CommitteeId),
                        nameof(RequisitionStatus.CHURCH_AUTH_PENDING) =>
                            (new[] { nameof(UserRole.Treasurer), nameof(UserRole.Accountant), nameof(UserRole.ChurchAuth), nameof(UserRole.Secretary) }, null, null, null),
                        nameof(RequisitionStatus.MINISTER_PENDING) =>
                            (new[] { nameof(UserRole.Minister) }, null, null, null),
                        _ => (Array.Empty<string>(), null, null, null)
                    };
                    await _approvalNotifications.NotifyAsync("requisitions.approve", "Requisition",
                        requisition.RequisitionNumber, $"ZMW {requisition.Amount:N2} requires approval.",
                        $"requisitions/approvals/{requisition.Id}", requisition.RequisitorId,
                        allowedRoles: allowedRoles, groupId: groupId, sectionId: sectionId, committeeId: committeeId,
                        ccUserId: requisition.RequisitorId, ccRoles: ccRoles.ToArray());
                }

                if (!requisition.RequisitorId.HasValue)
                    return;

                var requisitor = await _context.Users.FindAsync(requisition.RequisitorId.Value);
                if (requisitor == null || string.IsNullOrWhiteSpace(requisitor.Email))
                    return;

                await _emailService.SendRequisitionStatusChangedAsync(
                    requisitor.Email,
                    $"{requisitor.FirstName} {requisitor.LastName}",
                    requisition.RequisitionNumber,
                    requisition.Status,
                    note);

                await _logWriter.WriteAsync(new SystemLog
                {
                    Level = "Information",
                    Category = "Process",
                    Process = "Requisitions",
                    Action = "StatusChange",
                    UserId = requisition.UpdatedById,
                    Message = $"Requisition {requisition.RequisitionNumber} status changed to {requisition.Status} by {actorName}."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send requisition status-change notification for {RequisitionId}", requisition.Id);
            }
        }

        private async Task<Requisition> LoadOwnedRequisitionAsync(Guid id, Guid userId)
        {
            var requisition = await _context.Requisitions
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsArchived);

            if (requisition == null)
                throw new KeyNotFoundException("Requisition not found.");

            if (requisition.RequisitorId != userId)
                throw new UnauthorizedAccessException("Access denied.");

            return requisition;
        }

        private async Task<Requisition> LoadRequisitionAsync(Guid id)
        {
            var requisition = await _context.Requisitions
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsArchived);

            if (requisition == null)
                throw new KeyNotFoundException("Requisition not found.");

            return requisition;
        }

        // ============================================
        // FILE STORAGE HELPERS
        // ============================================

        private string GetFileStorageBasePath()
            => _configuration["FileStorage:BasePath"] ?? "uploads";

        private string GetRequisitionSubfolder()
            => _configuration["FileStorage:RequisitionSubfolder"] ?? "requisitions";

        private string GetRelativeFolderPath(Guid requisitionId)
        {
            var basePath = GetFileStorageBasePath().TrimStart('/').TrimEnd('/');
            var subFolder = GetRequisitionSubfolder().TrimStart('/').TrimEnd('/');
            return $"/{basePath}/{subFolder}/{requisitionId}";
        }

        private string GetPhysicalPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return string.Empty;
            var trimmed = relativePath.TrimStart('/');
            return _fileStorage.GetPhysicalPath(trimmed);
        }

        // ============================================
        // ATTACHMENT METHODS
        // ============================================

        public async Task<List<RequisitionAttachmentResponse>> UploadAttachmentsAsync(Guid requisitionId, List<IFormFile> files, Guid userId)
        {
            var requisition = await _context.Requisitions
                .FirstOrDefaultAsync(r => r.Id == requisitionId && !r.IsArchived);

            if (requisition == null)
                throw new KeyNotFoundException("Requisition not found");

            var relativeFolder = GetRelativeFolderPath(requisitionId);
            var physicalFolder = GetPhysicalPath(relativeFolder);
            Directory.CreateDirectory(physicalFolder);

            var attachments = new List<RequisitionAttachmentResponse>();

            foreach (var file in files)
            {
                if (file.Length == 0)
                    continue;

                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    throw new InvalidOperationException($"File type {extension} is not allowed. Allowed: PDF, JPG, JPEG, PNG, TIFF");

                if (file.Length > 10 * 1024 * 1024)
                    throw new InvalidOperationException("File size exceeds 10MB limit");

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(physicalFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = $"{relativeFolder}/{fileName}";

                var attachment = new RequisitionAtm
                {
                    Id = Guid.NewGuid(),
                    EntityId = requisitionId,
                    FileName = file.FileName,
                    FilePath = relativePath,
                    MimeType = file.ContentType,
                    Size = file.Length,
                    UploadedById = userId,
                    UploadedAt = DateTime.UtcNow,
                    Description = $"Uploaded by user {userId}",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    Version = 1
                };

                _context.RequisitionAtms.Add(attachment);
                await _context.SaveChangesAsync();

                attachments.Add(new RequisitionAttachmentResponse
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

        public async Task<List<RequisitionAttachmentResponse>> GetAttachmentsAsync(Guid requisitionId)
        {
            return await _context.RequisitionAtms
                .Where(a => a.EntityId == requisitionId)
                .OrderByDescending(a => a.UploadedAt)
                .Select(a => new RequisitionAttachmentResponse
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

        public async Task DeleteAttachmentAsync(Guid requisitionId, Guid attachmentId, Guid userId)
        {
            var attachment = await _context.RequisitionAtms
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.EntityId == requisitionId);

            if (attachment == null)
                throw new KeyNotFoundException("Attachment not found");

            var physicalPath = GetPhysicalPath(attachment.FilePath);
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            _context.RequisitionAtms.Remove(attachment);
            await _context.SaveChangesAsync();
        }

        // ============================================
        // PRIVATE FINAL APPROVE
        // ============================================

        private async Task<Requisition> FinalApproveRequisitionAsync(Requisition requisition, Guid userId)
        {
            var balance = await GetCurrentBalance(requisition.GroupId, requisition.SectionId, requisition.CommitteeId);
            if (balance == null)
                throw new InvalidOperationException("No fund balance found for this group.");

            var fundSource = Enum.Parse<FundSource>(requisition.FundSource);

            if (fundSource == FundSource.Budgeted)
            {
                if (requisition.Amount > balance.BudgetedBalance)
                    throw new InvalidOperationException($"Insufficient budget balance. Available: {balance.BudgetedBalance}, Requested: {requisition.Amount}");
                balance.BudgetedBalance -= requisition.Amount;
                balance.CommittedBudgetedBalance -= requisition.Amount;
            }
            else if (fundSource == FundSource.OwnFunds)
            {
                if (requisition.Amount > balance.OwnFundsBalance)
                    throw new InvalidOperationException($"Insufficient own funds balance. Available: {balance.OwnFundsBalance}, Requested: {requisition.Amount}");
                balance.OwnFundsBalance -= requisition.Amount;
                balance.CommittedOwnFundsBalance -= requisition.Amount;
            }

            balance.UpdatedDate = DateTime.UtcNow;
            balance.Version++;
            _context.FundBalances.Update(balance);

            requisition.Status = RequisitionStatus.APPROVED.ToString();
            requisition.ApprovedById = userId;
            requisition.ApprovedAt = DateTime.UtcNow;
            requisition.UpdatedDate = DateTime.UtcNow;
            requisition.UpdatedById = userId;
            requisition.LastProcessedTimeStamp = DateTime.UtcNow;
            requisition.Version++;
            await _context.SaveChangesAsync();

            var approver = await _context.Users.FindAsync(userId);
            await NotifyRequisitionStatusChangedAsync(
                requisition,
                approver != null ? $"{approver.FirstName} {approver.LastName}" : "an administrator");

            return requisition;
        }
    }
}
