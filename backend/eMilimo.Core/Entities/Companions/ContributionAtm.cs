using System;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities.Companions
{
    [Table("contribution_atms")]
    public class ContributionAtm : AttachmentCompanionBase
    {
        [ForeignKey(nameof(EntityId))]
        public Contribution? Contribution { get; set; }
    }
}
