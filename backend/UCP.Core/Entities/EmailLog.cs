using System;
using System.ComponentModel.DataAnnotations;

namespace UCP.Core.Entities
{
    /// <summary>
    /// Records every outbound email attempt made through IEmailService,
    /// whether it succeeded or failed, so administrators can see delivery
    /// history and diagnose failures from System Settings without needing
    /// server/file log access. Never stores the SMTP password or other
    /// credentials.
    /// </summary>
    public class EmailLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string ToEmail { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>e.g. PasswordReset, AccountActivation, RequisitionStatusChange, TestEmail.</summary>
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        /// <summary>"Success" or "Failed".</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Success";

        /// <summary>Failure reason (exception message only. Never credentials or the email body).</summary>
        [MaxLength(1000)]
        public string? FailureReason { get; set; }

        public Guid? TriggeredByUserId { get; set; }
    }
}