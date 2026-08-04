using System;
using System.ComponentModel.DataAnnotations.Schema;
using UCP.Core.Entities.Base;

namespace UCP.Core.Entities.Companions
{
    [Table("contribution_lcks")]
    public class ContributionLck : LockCompanionBase
    {
        [ForeignKey(nameof(EntityId))]
        public Contribution? Contribution { get; set; }
    }
}
