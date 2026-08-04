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

namespace UCP.API.Services
{
    public class ContributionService : IContributionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ContributionService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IGroupAccessService _groupAccess;

        public ContributionService(
            AppDbContext context,
            ILogger<ContributionService> logger,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IGroupAccessService groupAccess)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _groupAccess = groupAccess;
        }

        // ============================================
        // FILE STORAGE HELPERS (fully configurable)
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
            return Path.Combine(_webHostEnvironment.WebRootPath, trimmed);
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

            if (user.Role != UserRole.SuperAdmin.ToString() && user.Role != UserRole.Admin.ToString())
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
            var totalAllocated = request.Allocations.Sum(a => a.AllocatedAmount);
            if (Math.Abs(totalAllocated - request.Amount) > 0.01m)
            {
                throw new InvalidOperationException("Total allocated amount must equal contribution amount");
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

            await using var transaction = await _context.Database.BeginTransactionAsync();

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

                // Handle file uploads if any
                if (request.Files != null && request.Files.Any())
                {
                    await UploadAttachmentsAsync(contribution.Id, request.Files, userId);
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
                .FirstOrDefaultAsync(c => c.Id == contributionId);

            if (contribution == null)
                throw new KeyNotFoundException("Contribution not found");

            if (contribution.Status != ContributionStatus.PENDING.ToString())
                throw new InvalidOperationException("Only pending contributions can be rejected");

            contribution.Status = ContributionStatus.REJECTED.ToString();
            contribution.RejectionReason = dto.Reason;
            contribution.UpdatedById = userId;
            contribution.UpdatedDate = DateTime.UtcNow;
            contribution.Version++;

            _context.Contributions.Update(contribution);
            await _context.SaveChangesAsync();

            return await GetContributionByIdAsync(contributionId)
                ?? throw new Exception("Failed to retrieve rejected contribution");
        }

        // ============================================
        // UPDATE
        // ============================================

        public async Task<ContributionResponse> UpdateContributionAsync(Guid contributionId, UpdateContributionDto dto, Guid userId)
        {
            var contribution = await _context.Contributions
                .Include(c => c.Allocations)
                .FirstOrDefaultAsync(c => c.Id == contributionId);

            if (contribution == null)
                throw new KeyNotFoundException("Contribution not found");

            if (contribution.Status != ContributionStatus.DRAFT.ToString())
                throw new InvalidOperationException("Only draft contributions can be updated");

            // Check version for concurrency
            if (dto.Version.HasValue && dto.Version.Value != contribution.Version)
            {
                throw new DbUpdateConcurrencyException(
                    $"This contribution has been modified by another user. Current version: {contribution.Version}, Your version: {dto.Version}"
                );
            }

            var effectiveAmount = dto.Amount ?? contribution.Amount;

            if (dto.ContributionTypeId.HasValue)
            {
                var contributionType = await _context.ContributionTypes.FindAsync(dto.ContributionTypeId.Value);
                if (contributionType == null)
                    throw new KeyNotFoundException($"Contribution type with ID {dto.ContributionTypeId.Value} not found");
                contribution.ContributionTypeId = dto.ContributionTypeId.Value;
            }

            if (dto.SourceId.HasValue)
            {
                var source = await _context.ContributionSources.FindAsync(dto.SourceId.Value);
                if (source == null)
                    throw new KeyNotFoundException($"Source with ID {dto.SourceId.Value} not found");
                contribution.SourceId = dto.SourceId.Value;
            }

            if (dto.Allocations != null && dto.Allocations.Any())
            {
                var totalAllocated = dto.Allocations.Sum(a => a.AllocatedAmount);
                if (Math.Abs(totalAllocated - effectiveAmount) > 0.01m)
                    throw new InvalidOperationException($"Total allocated amount ({totalAllocated:F2}) must equal contribution amount ({effectiveAmount:F2})");

                foreach (var alloc in dto.Allocations)
                {
                    if (new[] { alloc.GroupId, alloc.SectionId, alloc.CommitteeId }.Count(x => x.HasValue) != 1)
                        throw new InvalidOperationException("Each allocation must select exactly one contributing entity.");
                }

                _context.ContributionAllocations.RemoveRange(contribution.Allocations);

                var newAllocations = dto.Allocations.Select(a => new ContributionAllocation
                {
                    Id = Guid.NewGuid(),
                    ContributionId = contribution.Id,
                    GroupId = a.GroupId,
                    SectionId = a.SectionId,
                    CommitteeId = a.CommitteeId,
                    AllocatedAmount = a.AllocatedAmount,
                    PercentageAllocation = a.PercentageAllocation ?? (a.AllocatedAmount / effectiveAmount * 100),
                    IsPrimary = a.IsPrimary,
                    Notes = a.Notes,
                    CreatedById = userId,
                    CreatedDate = DateTime.UtcNow,
                    Version = 1
                }).ToList();

                contribution.Allocations = newAllocations;
                _context.ContributionAllocations.AddRange(newAllocations);
            }

            if (dto.Amount.HasValue)
                contribution.Amount = dto.Amount.Value;

            if (!string.IsNullOrEmpty(dto.PaymentMethod))
                contribution.PaymentMethod = dto.PaymentMethod;

            if (dto.Description != null)
                contribution.Description = dto.Description;

            if (dto.ReferenceNumber != null)
                contribution.ReferenceNumber = dto.ReferenceNumber;

            if (dto.BankAccountId.HasValue)
                contribution.BankAccountId = dto.BankAccountId.Value;

            contribution.UpdatedById = userId;
            contribution.UpdatedDate = DateTime.UtcNow;
            contribution.Version++;

            // This contribution was loaded by the current context and is already
            // tracked. Update() would traverse allocations with assigned GUIDs and
            // incorrectly classify those new rows as Modified rather than Added.
            await _context.SaveChangesAsync();

            return await GetContributionByIdAsync(contributionId)
                ?? throw new Exception("Failed to retrieve updated contribution");
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
        // GET BY ID
        // ============================================

        public async Task<ContributionResponse?> GetContributionByIdAsync(Guid id)
        {
            var accessibleIds = await _groupAccess.GetAccessibleGroupIdsAsync();
            var accessibleSections = await _groupAccess.GetAccessibleSectionIdsAsync();
            var accessibleCommittees = await _groupAccess.GetAccessibleCommitteeIdsAsync();
            var hasGlobalAccess = _groupAccess.HasGlobalAccess();
            var contribution = await _context.Contributions
                .Include(c => c.ContributionType)
                .Include(c => c.Source)
                .Include(c => c.Allocations)
                    .ThenInclude(a => a.Group)
                .Include(c => c.ReceivedByUser)
                .Include(c => c.ConfirmedByUser)
                .Include(c => c.CreatedByUser)
                .Include(c => c.UpdatedByUser)
                .Include(c => c.Attachments)
                .Include(c => c.BankAccount)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsArchived &&
                    (hasGlobalAccess || c.Allocations.Any(a =>
                        (a.GroupId.HasValue && accessibleIds.Contains(a.GroupId.Value)) ||
                        (a.SectionId.HasValue && accessibleSections.Contains(a.SectionId.Value)) ||
                        (a.CommitteeId.HasValue && accessibleCommittees.Contains(a.CommitteeId.Value)))));

            return contribution != null ? MapToResponse(contribution) : null;
        }

        // ============================================
        // LIST
        // ============================================

        public async Task<(List<ContributionResponse> Data, int Total)> GetContributionsAsync(ContributionFilter filter)
        {
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
                .Include(c => c.Allocations).ThenInclude(a => a.Section)
                .Include(c => c.Allocations).ThenInclude(a => a.Committee)
                .Include(c => c.ReceivedByUser)
                .Include(c => c.ConfirmedByUser)
                .Where(c => !c.IsArchived);

            if (!_groupAccess.HasGlobalAccess())
                query = query.Where(c => c.Allocations.Any(a =>
                    (a.GroupId.HasValue && accessibleIds.Contains(a.GroupId.Value)) ||
                    (a.SectionId.HasValue && accessibleSections.Contains(a.SectionId.Value)) ||
                    (a.CommitteeId.HasValue && accessibleCommittees.Contains(a.CommitteeId.Value))));

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
            {
                query = query.Where(c => c.Allocations.Any(a => a.GroupId == filter.GroupId.Value));
            }
            if (filter.SectionId.HasValue)
                query = query.Where(c => c.Allocations.Any(a => a.SectionId == filter.SectionId.Value));
            if (filter.CommitteeId.HasValue)
                query = query.Where(c => c.Allocations.Any(a => a.CommitteeId == filter.CommitteeId.Value));

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(c => c.CreatedDate)
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
            var accessibleIds = await _groupAccess.GetAccessibleGroupIdsAsync();
            var hasGlobalAccess = _groupAccess.HasGlobalAccess();
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
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .Select(t => new ContributionTypeResponse
                {
                    Id = t.Id,
                    Code = t.Code,
                    Name = t.Name,
                    Description = t.Description
                })
                .ToListAsync();
        }

        public async Task<List<ContributionSourceResponse>> GetContributionSourcesAsync()
        {
            return await _context.ContributionSources
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new ContributionSourceResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description
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
        // ATTACHMENT METHODS (FULLY CONFIGURABLE)
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
                    Description = c.ContributionType.Description
                } : null,
                Source = c.Source != null ? new ContributionSourceResponse
                {
                    Id = c.Source.Id,
                    Name = c.Source.Name,
                    Description = c.Source.Description
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
