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

namespace UCP.API.Services
{
    public class RequisitionService : IRequisitionService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<RequisitionService> _logger;
        private readonly IGroupAccessService _groupAccess;

        public RequisitionService(
            AppDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            ILogger<RequisitionService> logger,
            IGroupAccessService groupAccess)
        {
            _context = context;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _groupAccess = groupAccess;
        }

        // ============================================
        // STATS
        // ============================================

        public async Task<RequisitionStatsDto> GetStatsAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new RequisitionStatsDto();

            var query = _context.Requisitions.AsQueryable();

            if (user.Role != UserRole.SuperAdmin.ToString() && user.Role != UserRole.Admin.ToString())
            {
                var groups = await _groupAccess.GetAccessibleGroupIdsAsync();
                var sections = await _groupAccess.GetAccessibleSectionIdsAsync();
                var committees = await _groupAccess.GetAccessibleCommitteeIdsAsync();
                query = query.Where(r => (r.GroupId.HasValue && groups.Contains(r.GroupId.Value)) ||
                    (r.SectionId.HasValue && sections.Contains(r.SectionId.Value)) ||
                    (r.CommitteeId.HasValue && committees.Contains(r.CommitteeId.Value)));
            }

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

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            if (user.Role != UserRole.SuperAdmin.ToString() && user.Role != UserRole.Admin.ToString())
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

            if (user.Role != UserRole.SuperAdmin.ToString() && user.Role != UserRole.Admin.ToString())
            {
                var groups = await _groupAccess.GetAccessibleGroupIdsAsync();
                var sections = await _groupAccess.GetAccessibleSectionIdsAsync();
                var committees = await _groupAccess.GetAccessibleCommitteeIdsAsync();
                query = query.Where(r => (r.GroupId.HasValue && groups.Contains(r.GroupId.Value)) ||
                    (r.SectionId.HasValue && sections.Contains(r.SectionId.Value)) ||
                    (r.CommitteeId.HasValue && committees.Contains(r.CommitteeId.Value)));
            }

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

            return await query.ToListAsync();
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
                .Include(r => r.Items)
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

            bool isRequisitor = requisition.RequisitorId == userId;
            bool isAdmin = user.Role == UserRole.SuperAdmin.ToString() || user.Role == UserRole.Admin.ToString();
            if (!isRequisitor && !isAdmin)
            {
                throw new UnauthorizedAccessException("You are not authorized to update this requisition. Only the requisitor or an admin can edit it.");
            }

            if (requisition.Status != RequisitionStatus.DRAFT.ToString())
                throw new InvalidOperationException("Only draft requisitions can be updated");

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
                if (requisition.Items != null && requisition.Items.Any())
                {
                    _context.RequisitionItems.RemoveRange(requisition.Items);
                }

                var newItems = dto.Items.Select(item => new RequisitionItem
                {
                    Id = Guid.NewGuid(),
                    RequisitionId = requisition.Id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Total = item.Quantity * item.UnitPrice,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    Version = 1
                }).ToList();

                requisition.Items = newItems;
                _context.RequisitionItems.AddRange(newItems);
                requisition.Amount = newItems.Sum(i => i.Total);
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
                await _context.SaveChangesAsync();
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
            if (fundSource == FundSource.Budgeted)
            {
                var availableBudgeted = balance.BudgetedBalance - balance.CommittedBudgetedBalance;
                if (requisition.Amount > availableBudgeted)
                    throw new InvalidOperationException($"Insufficient budget balance. Available: {availableBudgeted}, Requested: {requisition.Amount}");
                balance.CommittedBudgetedBalance += requisition.Amount;
            }
            else if (fundSource == FundSource.OwnFunds)
            {
                var availableOwnFunds = balance.OwnFundsBalance - balance.CommittedOwnFundsBalance;
                if (requisition.Amount > availableOwnFunds)
                    throw new InvalidOperationException($"Insufficient own funds balance. Available: {availableOwnFunds}, Requested: {requisition.Amount}");
                balance.CommittedOwnFundsBalance += requisition.Amount;
            }

            balance.UpdatedDate = DateTime.UtcNow;
            balance.Version++;
            _context.FundBalances.Update(balance);

            requisition.Status = RequisitionStatus.SENIOR_REPS_PENDING.ToString();
            requisition.UpdatedDate = DateTime.UtcNow;
            requisition.UpdatedById = userId;
            requisition.LastProcessedTimeStamp = DateTime.UtcNow;
            requisition.Version++;
            await _context.SaveChangesAsync();

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
            return requisition.FirstSeniorApproverId == userId || requisition.SecondSeniorApproverId == userId;
        }

        public async Task<Requisition> ApproveRequisitionAsync(Guid id, ApproveRequisitionDto dto, Guid userId)
        {
            var requisition = await LoadRequisitionAsync(id);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            if (requisition.Status == RequisitionStatus.REJECTED.ToString() || requisition.Status == RequisitionStatus.ARCHIVED.ToString())
                throw new InvalidOperationException("Cannot approve a requisition in its current state.");

            // SuperAdmin or Admin can approve directly (bypass rules)
            if (user.Role == UserRole.SuperAdmin.ToString() || user.Role == UserRole.Admin.ToString())
            {
                return await FinalApproveRequisitionAsync(requisition, userId);
            }

            var currentStatus = requisition.Status;

            if (currentStatus == RequisitionStatus.SENIOR_REPS_PENDING.ToString())
            {
                if (user.Role != UserRole.SeniorRep.ToString())
                    throw new UnauthorizedAccessException("Only Senior Representatives can approve at this stage.");

                if (HasUserApproved(requisition, userId))
                    throw new InvalidOperationException("You have already approved this requisition.");

                if (!requisition.FirstSeniorApproverId.HasValue)
                {
                    requisition.FirstSeniorApproverId = userId;
                }
                else if (!requisition.SecondSeniorApproverId.HasValue)
                {
                    requisition.SecondSeniorApproverId = userId;
                }
                else
                {
                    throw new InvalidOperationException("Both senior approval slots are already filled.");
                }

                if (GetSeniorApprovalCount(requisition) >= 2)
                {
                    requisition.Status = RequisitionStatus.CHURCH_AUTH_PENDING.ToString();
                }

                requisition.UpdatedDate = DateTime.UtcNow;
                requisition.UpdatedById = userId;
                requisition.LastProcessedTimeStamp = DateTime.UtcNow;
                requisition.Version++;
                await _context.SaveChangesAsync();

                return requisition;
            }

            if (currentStatus == RequisitionStatus.CHURCH_AUTH_PENDING.ToString())
            {
                if (user.Role != UserRole.ChurchAuth.ToString())
                    throw new UnauthorizedAccessException("Only Church Auth representatives can approve at this stage.");

                requisition.Status = RequisitionStatus.MINISTER_PENDING.ToString();
                requisition.UpdatedDate = DateTime.UtcNow;
                requisition.UpdatedById = userId;
                requisition.LastProcessedTimeStamp = DateTime.UtcNow;
                requisition.Version++;
                await _context.SaveChangesAsync();

                return requisition;
            }

            if (currentStatus == RequisitionStatus.MINISTER_PENDING.ToString())
            {
                if (user.Role != UserRole.Minister.ToString())
                    throw new UnauthorizedAccessException("Only Minister can approve at this stage.");

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
                if (currentStatus == RequisitionStatus.SENIOR_REPS_PENDING.ToString() && user.Role != UserRole.SeniorRep.ToString())
                    throw new UnauthorizedAccessException("You are not authorized to reject at this stage.");
                else if (currentStatus == RequisitionStatus.CHURCH_AUTH_PENDING.ToString() && user.Role != UserRole.ChurchAuth.ToString())
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
            return Path.Combine(_webHostEnvironment.WebRootPath, trimmed);
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

            return requisition;
        }
    }
}
