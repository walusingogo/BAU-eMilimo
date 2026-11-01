using System.ComponentModel.DataAnnotations;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

public static class AuthorityScopeTypes
{
    public const string Entity = "ENTITY";
    public const string Section = "SECTION";
    public const string Group = "GROUP";
    public const string Committee = "COMMITTEE";
    public const string FunctionalArea = "FUNCTIONAL_AREA";
    public const string Congregation = "CONGREGATION";
    public static readonly string[] All = [Entity, Section, Group, Committee, FunctionalArea, Congregation];
}

/// <summary>An effective-dated grant of church-office authority. Ordinary membership is stored separately.</summary>
public class OfficeAppointment : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid DesignationId { get; set; }
    public Guid FunctionalAreaId { get; set; }
    [MaxLength(30)] public string ScopeType { get; set; } = AuthorityScopeTypes.Congregation;
    public Guid? SectionId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? CommitteeId { get; set; }
    public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveToUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? AssignedById { get; set; }

    public User User { get; set; } = null!;
    public Designation Designation { get; set; } = null!;
    public FunctionalArea FunctionalArea { get; set; } = null!;
    public Section? Section { get; set; }
    public Group? Group { get; set; }
    public Committee? Committee { get; set; }
    public User? AssignedBy { get; set; }
}
