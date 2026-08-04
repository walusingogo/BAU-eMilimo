using System;
using System.ComponentModel.DataAnnotations.Schema;
using UCP.Core.Entities.Base;

namespace UCP.Core.Entities.Companions
{
    public abstract class LockCompanionBase : BaseEntity
    {
        public Guid EntityId { get; set; }
        public Guid LockedById { get; set; }
        public DateTime LockedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? LockReason { get; set; }

        [ForeignKey(nameof(LockedById))]
        public virtual User? LockedByUser { get; set; }
    }
}
