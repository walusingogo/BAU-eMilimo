using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCP.Core.Entities;

[Table("logs")]
public class SystemLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(20)] public string Level { get; set; } = "Information";
    [MaxLength(20)] public string Category { get; set; } = "Process";
    [MaxLength(100)] public string Process { get; set; } = string.Empty;
    [MaxLength(150)] public string Action { get; set; } = string.Empty;
    [MaxLength(10)] public string? HttpMethod { get; set; }
    [MaxLength(500)] public string? Path { get; set; }
    public int? StatusCode { get; set; }
    public long? DurationMilliseconds { get; set; }
    public Guid? UserId { get; set; }
    [MaxLength(100)] public string? MembershipId { get; set; }
    [MaxLength(100)] public string? UserRole { get; set; }
    [MaxLength(100)] public string? IpAddress { get; set; }
    [MaxLength(100)] public string CorrelationId { get; set; } = string.Empty;
    [MaxLength(2000)] public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? Exception { get; set; }
}
