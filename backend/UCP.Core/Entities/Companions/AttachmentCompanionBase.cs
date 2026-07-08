using System;
using UCP.Core.Entities.Base;

namespace UCP.Core.Entities.Companions
{
    public abstract class AttachmentCompanionBase : BaseEntity
    {
        public Guid EntityId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long Size { get; set; }
        public Guid? UploadedById { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string? Description { get; set; }
    }
}