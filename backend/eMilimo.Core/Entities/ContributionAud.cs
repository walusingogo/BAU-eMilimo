using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eMilimo.Core.Entities;

[Table("contribution_aud")]
public class ContributionAud
{
    public Guid Id { get; set; }
    public Guid ContributionId { get; set; }
    [MaxLength(50)] public string Action { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = "{}";
    public Guid? PerformedById { get; set; }
    public DateTime PerformedAtUtc { get; set; }
}
