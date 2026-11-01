using System.ComponentModel.DataAnnotations;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

public static class DesignationCodes
{
    public const string StewardshipConvener = "STEWARDSHIP_CONVENER";
    public const string Elder = "ELDER";
    public const string Steward = "STEWARD";
    public const string SundaySchoolTeacher = "SUNDAY_SCHOOL_TEACHER";
    public const string Minister = "MINISTER";
    public const string Treasurer = "TREASURER";
    public const string Accountant = "ACCOUNTANT";
    public const string Convener = "CONVENER";
    public const string ViceConvener = "VICE_CONVENER";
    public const string ChurchSecretary = "CHURCH_SECRETARY";
    public const string Chairperson = "CHAIRPERSON";
    public const string ViceChairperson = "VICE_CHAIRPERSON";
    public const string Secretary = "SECRETARY";
    public const string ViceSecretary = "VICE_SECRETARY";
    public const string Teacher = "TEACHER";
    public const string Representative = "REPRESENTATIVE";
    public const string CommitteeMember = "COMMITTEE_MEMBER";
    public const string Leader = "LEADER";
    public const string Member = "MEMBER";
    public const string Other = "OTHER";
}

/// <summary>
/// A functional appointment held under an authority role. Designations describe
/// what work a user performs; the user's section/group remains their home
/// membership and does not limit organization-wide functional jurisdiction.
/// </summary>
public class Designation : BaseEntity
{
    [MaxLength(80)] public string Code { get; set; } = string.Empty;
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(30)] public string RequiredRole { get; set; } = string.Empty;
    [MaxLength(80)] public string FunctionalJurisdiction { get; set; } = string.Empty;
    public Guid? FunctionalAreaId { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public string CapabilitiesJson { get; set; } = "[]";
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;

    public FunctionalArea? FunctionalArea { get; set; }
    public ICollection<OfficeAppointment> OfficeAppointments { get; set; } = new List<OfficeAppointment>();
}
