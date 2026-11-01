using System.ComponentModel.DataAnnotations;

namespace eMilimo.Core.Entities;

public sealed class CommunicationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(20)] public string Channel { get; set; } = "";
    [MaxLength(50)] public string Provider { get; set; } = "";
    [MaxLength(255)] public string Recipient { get; set; } = "";
    [MaxLength(100)] public string Action { get; set; } = "General";
    [MaxLength(255)] public string? Subject { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Failed";
    [MaxLength(100)] public string? ProviderMessageId { get; set; }
    [MaxLength(1000)] public string? FailureReason { get; set; }
    public string? ProtectedMessage { get; set; }
    public string? ProtectedCcRecipients { get; set; }
    public Guid? RetryOfCommunicationLogId { get; set; }
    public Guid? TriggeredByUserId { get; set; }
}
