using System.ComponentModel.DataAnnotations;

namespace UCP.Core.Entities;

public sealed class CommunicationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(20)] public string Channel { get; set; } = "";
    [MaxLength(50)] public string Provider { get; set; } = "Twilio";
    [MaxLength(40)] public string Recipient { get; set; } = "";
    [MaxLength(100)] public string Action { get; set; } = "General";
    [MaxLength(20)] public string Status { get; set; } = "Failed";
    [MaxLength(100)] public string? ProviderMessageId { get; set; }
    [MaxLength(1000)] public string? FailureReason { get; set; }
    public string? ProtectedMessage { get; set; }
    public Guid? RetryOfCommunicationLogId { get; set; }
}
