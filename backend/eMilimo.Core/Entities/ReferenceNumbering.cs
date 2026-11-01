using System.ComponentModel.DataAnnotations;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

public sealed class ReferenceNumberSequence
{
    [MaxLength(30)] public string ReferenceType { get; set; } = string.Empty;
    public DateOnly SequenceDate { get; set; }
    public long LastValue { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class ReferenceNumberConfiguration : BaseEntity
{
    [MaxLength(30)] public string ReferenceType { get; set; } = string.Empty;
    [MaxLength(10)] public string Prefix { get; set; } = string.Empty;
    [MaxLength(200)] public string Description { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public bool IsActive { get; set; } = true;
}
