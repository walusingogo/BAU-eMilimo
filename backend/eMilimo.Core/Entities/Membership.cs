using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

public static class MembershipStatuses
{
    public const string Prospective = "PROSPECTIVE";
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
    public const string Transferred = "TRANSFERRED";
    public const string Deceased = "DECEASED";
    public const string Suspended = "SUSPENDED";
    public const string Archived = "ARCHIVED";
    public static readonly string[] All = [Prospective, Active, Inactive, Transferred, Deceased, Suspended, Archived];
}

[Table("members")]
public sealed class Member : BaseEntity
{
    [Required, MaxLength(50)] public string MembershipNumber { get; set; } = string.Empty;
    [MaxLength(20)] public string? Title { get; set; }
    [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
    [MaxLength(100)] public string? MiddleNames { get; set; }
    [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
    [MaxLength(200)] public string? PreferredName { get; set; }
    [MaxLength(30)] public string? Sex { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [MaxLength(50)] public string? PhoneNumber { get; set; }
    [MaxLength(255)] public string? Email { get; set; }
    [MaxLength(500)] public string? Address { get; set; }
    public Guid? SectionId { get; set; }
    public Section? Section { get; set; }
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }
    [MaxLength(50)] public string MembershipType { get; set; } = "MEMBER";
    [MaxLength(30)] public string Status { get; set; } = MembershipStatuses.Active;
    public DateOnly? JoinedOn { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public Guid? ArchivedById { get; set; }
    [MaxLength(500)] public string? ArchiveReason { get; set; }
    [MaxLength(30)] public string RecordSource { get; set; } = "LOCAL";
    [MaxLength(30)] public string SynchronizationStatus { get; set; } = "LOCAL_ONLY";
    public DateTime? LastSynchronizedAtUtc { get; set; }
    [MaxLength(200)] public string NormalizedSearchName { get; set; } = string.Empty;
    [MaxLength(50)] public string? NormalizedPhoneNumber { get; set; }
    public ICollection<MemberExternalIdentity> ExternalIdentities { get; set; } = new List<MemberExternalIdentity>();
    public ICollection<MembershipStatusHistory> StatusHistory { get; set; } = new List<MembershipStatusHistory>();
}

[Table("membership_status_history")]
public sealed class MembershipStatusHistory : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    [MaxLength(30)] public string PreviousStatus { get; set; } = string.Empty;
    [MaxLength(30)] public string NewStatus { get; set; } = string.Empty;
    public DateTime EffectiveAtUtc { get; set; }
    [MaxLength(500)] public string Reason { get; set; } = string.Empty;
    public Guid ChangedById { get; set; }
}

[Table("member_external_identities")]
public sealed class MemberExternalIdentity : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    [MaxLength(50)] public string ProviderCode { get; set; } = string.Empty;
    [MaxLength(200)] public string ExternalMemberId { get; set; } = string.Empty;
    [MaxLength(100)] public string? ExternalMembershipNumber { get; set; }
    [MaxLength(200)] public string? ExternalVersion { get; set; }
    public DateTime? LastSynchronizedAtUtc { get; set; }
}

[Table("membership_integration_configurations")]
public sealed class MembershipIntegrationConfiguration : BaseEntity
{
    [MaxLength(50)] public string ProviderCode { get; set; } = "LOCAL";
    [MaxLength(300)] public string? Endpoint { get; set; }
    public bool IsEnabled { get; set; }
    public bool UseLocalFallback { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 15;
    [MaxLength(30)] public string FieldOwnershipMode { get; set; } = "EXTERNAL_WITH_LOCAL_FALLBACK";
    public DateTime? LastSuccessfulSyncAtUtc { get; set; }
}

[Table("membership_sync_logs")]
public sealed class MembershipSyncLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(50)] public string ProviderCode { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "STARTED";
    public int ReadCount { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int ConflictCount { get; set; }
    public string? Error { get; set; }
    public Guid? InitiatedById { get; set; }
    [MaxLength(100)] public string CorrelationId { get; set; } = string.Empty;
}

[Table("member_aud")]
public sealed class MemberAud
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    [MaxLength(50)] public string Action { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = "{}";
    public Guid? PerformedById { get; set; }
    public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string CorrelationId { get; set; } = string.Empty;
}
