using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCP.Core.Entities;

[Table("requisition_aud")]
public class RequisitionAud
{
    public Guid Id { get; set; }
    public Guid RequisitionId { get; set; }
    [MaxLength(50)] public string Action { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = "{}";
    public Guid? PerformedById { get; set; }
    public DateTime PerformedAtUtc { get; set; }
}
