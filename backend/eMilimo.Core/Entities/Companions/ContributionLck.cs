using System;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities.Companions
{
    [Table("contribution_lcks")]
    public class ContributionLck : LockCompanionBase
    {
        [ForeignKey(nameof(EntityId))]
        public Contribution? Contribution { get; set; }
    }
}
