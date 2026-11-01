using System;

namespace eMilimo.Core.Entities.Base
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }

        // System timestamps
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastProcessedTimeStamp { get; set; }

        // Audit
        public Guid? CreatedById { get; set; }
        public Guid? UpdatedById { get; set; }

        // Optimistic locking
        public int Version { get; set; } = 1;
    }
}