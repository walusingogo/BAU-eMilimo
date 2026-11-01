using System.ComponentModel.DataAnnotations;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

public class FunctionalArea : BaseEntity
{
    [MaxLength(80)] public string Code { get; set; } = string.Empty;
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Designation> Designations { get; set; } = new List<Designation>();
    public ICollection<OfficeAppointment> OfficeAppointments { get; set; } = new List<OfficeAppointment>();
}

public static class FunctionalAreaCodes
{
    public static readonly string[] System =
    [
        "MINISTRY", "SECRETARIAT", "ACCOUNTING", "TREASURY", "FINANCE", "STEWARDSHIP",
        "DWE", "WCF", "MCF", "YOUTH", "SUNDAY_SCHOOL", "CHRISTIAN_TRAINING", "MEMBERSHIP",
        "CDSJC", "COMMUNICATIONS", "DIACONAL", "SECTION_ADMINISTRATION", "CHURCH_OVERSIGHT", "AUDIT", "OTHER"
    ];
}
