using System.ComponentModel.DataAnnotations;

namespace eMilimo.Core.DTOs;

public sealed class SaveMemberRequest
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
    [EmailAddress, MaxLength(255)] public string? Email { get; set; }
    [MaxLength(500)] public string? Address { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? GroupId { get; set; }
    [MaxLength(50)] public string MembershipType { get; set; } = "MEMBER";
    [MaxLength(30)] public string Status { get; set; } = "ACTIVE";
    public DateOnly? JoinedOn { get; set; }
    public int? Version { get; set; }
}

public sealed record ChangeMembershipStatusRequest(string Status, string Reason, int Version);
public sealed record ArchiveMemberRequest(string Reason, int Version);
public sealed record MergeMemberRequest(string Reason, int SourceVersion, int TargetVersion);
public sealed record SaveMembershipIntegrationRequest(string ProviderCode, string? Endpoint, bool IsEnabled, bool UseLocalFallback, int TimeoutSeconds, string FieldOwnershipMode, int? Version, string? ApiKey = null);

public sealed record ImportMemberRow(
    string MembershipNumber, string FirstName, string LastName, string? Title = null,
    string? MiddleNames = null, string? PreferredName = null, string? PhoneNumber = null,
    string? Email = null, string MembershipType = "MEMBER", string Status = "ACTIVE",
    string? Section = null, string? Group = null, DateOnly? JoinedOn = null,
    DateOnly? DateOfBirth = null, string? Sex = null, string? Address = null);

public sealed record ImportMembersRequest(bool DryRun, string Mode, IReadOnlyList<ImportMemberRow> Rows);
