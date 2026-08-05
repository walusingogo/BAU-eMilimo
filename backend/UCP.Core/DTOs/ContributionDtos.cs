// backend/UCP.Core/DTOs/ContributionDtos.cs
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using UCP.Core.Enums;

namespace UCP.Core.DTOs
{
    // ============ REQUEST DTOs ============
    
    public class CreateContributionRequest
    {
        [Required]
        public Guid ContributionTypeId { get; set; }

        [Required]
        public Guid SourceId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime ContributionDate { get; set; }

        public string? Description { get; set; }

        public string? ReferenceNumber { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public Guid? BankAccountId { get; set; }

        public bool IsSplit { get; set; } = false;

        public ContributionStatus Status { get; set; } = ContributionStatus.DRAFT;

        [Required]
        [MinLength(1, ErrorMessage = "At least one allocation is required")]
        public List<ContributionAllocationRequest> Allocations { get; set; } = new();
        
        public List<IFormFile>? Files { get; set; }
    }

    public class ContributionAllocationRequest
    {
        public Guid? GroupId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? CommitteeId { get; set; }

        [Required]
        public Guid ContributionTypeId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal AllocatedAmount { get; set; }

        [Range(0, 100)]
        public decimal? PercentageAllocation { get; set; }

        public bool IsPrimary { get; set; } = false;

        public string? Notes { get; set; }
    }

    // ============ UPDATE CONTRIBUTION DTO ============
    public class UpdateContributionDto
    {
        [Range(0.01, double.MaxValue)]
        public decimal? Amount { get; set; }

        public DateTime? ContributionDate { get; set; }

        public string? PaymentMethod { get; set; }

        public string? Description { get; set; }

        public string? ReferenceNumber { get; set; }

        public Guid? ContributionTypeId { get; set; }

        public Guid? SourceId { get; set; }

        public Guid? BankAccountId { get; set; }

        public bool? IsSplit { get; set; }

        public int? Version { get; set; }

        // Send the allocations from Angular as:
        // formData.append("AllocationsJson", JSON.stringify(allocations));
        // Null means that the current allocations must remain unchanged.
        public string? AllocationsJson { get; set; }

        // Populated by ContributionsController after AllocationsJson is parsed.
        // The service can continue reading dto.Allocations as before.
        [JsonIgnore]
        public List<ContributionAllocationRequest>? Allocations { get; set; }

        public List<IFormFile>? Files { get; set; }
    }

    // ============ CREATE DTOs for Types and Sources ============
    public class CreateContributionTypeDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class CreateContributionSourceDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    // ============ UPDATE DTOs for Types and Sources ============
    public class UpdateContributionTypeDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateContributionSourceDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    // ============ RESPONSE DTOs ============
    public class ContributionResponse
    {
        public Guid Id { get; set; }
        public string ContributionNumber { get; set; } = string.Empty;
        public ContributionTypeResponse? ContributionType { get; set; }
        public ContributionSourceResponse? Source { get; set; }
        public decimal Amount { get; set; }
        public DateTime ContributionDate { get; set; }
        public string? Description { get; set; }
        public string? ReferenceNumber { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsSplit { get; set; }
        public BankAccountResponse? BankAccount { get; set; }
        public int Version { get; set; }
        public List<ContributionAllocationResponse> Allocations { get; set; } = new();
        public UserBriefResponse? ReceivedBy { get; set; }
        public UserBriefResponse? ConfirmedBy { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string? RejectionReason { get; set; }
        public string? ArchiveReason { get; set; }
        public UserBriefResponse? CreatedBy { get; set; }
        public UserBriefResponse? UpdatedBy { get; set; }
        public List<ContributionAttachmentResponse> Attachments { get; set; } = new();
    }

    // FIXED: Added IsActive to the two response DTOs
    public class ContributionTypeResponse
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }   // <-- ADDED
    }

    public class ContributionSourceResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }   // <-- ADDED
    }

    public class ContributionAllocationResponse
    {
        public Guid Id { get; set; }
        public GroupBriefResponse? Group { get; set; }
        public OrganizationBriefResponse? Entity { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal? PercentageAllocation { get; set; }
        public bool IsPrimary { get; set; }
        public string? Notes { get; set; }
    }

    public class OrganizationBriefResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
    }

    public class GroupBriefResponse
    {
        public Guid Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string GroupType { get; set; } = string.Empty;
    }

    public class UserBriefResponse
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? SignatureData { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    // ============================================
    // ATTACHMENT DTOs
    // ============================================

    public class ContributionAttachmentResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long Size { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public Guid? UploadedBy { get; set; }
        public string? UploadedByName { get; set; }
        public string FileUrl { get; set; } = string.Empty;
    }

    public class UploadContributionAttachmentRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }

    // ============================================
    // REJECT & ARCHIVE DTOs
    // ============================================
    
    public class RejectContributionDto
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    public class ArchiveContributionDto
    {
        public string? Reason { get; set; }
    }

    // ============================================
    // STATS DTO
    // ============================================

    public class ContributionStatsDto
    {
        public int Total { get; set; }
        public int Draft { get; set; }
        public int Pending { get; set; }
        public int Received { get; set; }
        public int Confirmed { get; set; }
        public int Allocated { get; set; }
        public int Rejected { get; set; }
        public int Archived { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal ConfirmedAmount { get; set; }
    }

    // ============================================
    // FILTER DTO
    // ============================================

    public class ContributionFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? CommitteeId { get; set; }
        public Guid? TypeId { get; set; }
        public Guid? SourceId { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }
        public int Limit { get; set; } = 50;
        public int Offset { get; set; } = 0;
    }

    // ============================================
    // DASHBOARD STATS
    // ============================================

    public class ContributionDashboardStats
    {
        public ContributionSummary Summary { get; set; } = new();
        public List<ContributionByTypeStats> ByType { get; set; } = new();
        public List<ContributionBySourceStats> BySource { get; set; } = new();
        public List<ContributionByGroupStats> ByGroup { get; set; } = new();
        public List<RecentContributionResponse> Recent { get; set; } = new();
    }

    public class ContributionSummary
    {
        public int TotalContributions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public double PercentageChange { get; set; }
        public decimal PreviousPeriodTotal { get; set; }
    }

    public class ContributionByTypeStats
    {
        public string TypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ContributionBySourceStats
    {
        public string SourceName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ContributionByGroupStats
    {
        public string GroupName { get; set; } = string.Empty;
        public int AllocationCount { get; set; }
        public decimal TotalAllocated { get; set; }
    }

    public class RecentContributionResponse
    {
        public Guid Id { get; set; }
        public string ContributionNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ContributionDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Groups { get; set; } = new();
    }

    public class BankAccountResponse
    {
        public Guid Id { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string? Branch { get; set; }
        public string? AccountType { get; set; }
    }
}