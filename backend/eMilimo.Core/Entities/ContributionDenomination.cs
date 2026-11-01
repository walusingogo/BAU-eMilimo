using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

[Table("contribution_denominations")]
public sealed class ContributionDenomination : BaseEntity
{
    public Guid ContributionId { get; set; }
    public Contribution? Contribution { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Denomination { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
}
