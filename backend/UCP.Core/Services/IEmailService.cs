using System.Threading;
using System.Threading.Tasks;

namespace UCP.Core.Services
{
    /// <summary>
    /// Centralized email notification service. All outbound application email
    /// must go through this interface rather than embedding SMTP logic
    /// directly in controllers or business services.
    ///
    /// Implementations must never throw for delivery failures — a failed email
    /// must be logged, not allowed to break the business operation that
    /// triggered it (e.g. a requisition approval must still succeed even if
    /// the notification email fails to send).
    /// </summary>
    public interface IEmailService
    {
        Task SendPasswordResetAsync(string toEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default);

        Task SendTemporaryPasswordAsync(string toEmail, string recipientName, string temporaryPassword, CancellationToken cancellationToken = default);

        Task SendAccountActivatedAsync(string toEmail, string recipientName, CancellationToken cancellationToken = default);

        Task SendAccountDeactivatedAsync(string toEmail, string recipientName, CancellationToken cancellationToken = default);

        Task SendRequisitionStatusChangedAsync(
            string toEmail,
            string recipientName,
            string requisitionNumber,
            string newStatus,
            string? note,
            CancellationToken cancellationToken = default);

        Task SendContributionStatusChangedAsync(
            string toEmail,
            string recipientName,
            string contributionNumber,
            string newStatus,
            string? note,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// General-purpose escape hatch for other meaningful notifications
        /// (e.g. account created with temporary password) that don't warrant
        /// their own strongly-typed method yet.
        /// </summary>
        Task SendAssetRequestStatusChangedAsync(string toEmail, string recipientName, string requestNumber,
            string assetName, int quantity, string newStatus, DateTime expectedOutAtUtc,
            DateTime expectedReturnAtUtc, bool paymentRequired, decimal? paymentAmount, string? note,
            CancellationToken cancellationToken = default);
        Task SendApprovalActionRequiredAsync(string toEmail, string recipientName, string processName,
            string recordNumber, string summary, string approvalUrl, string[] actions,
            IReadOnlyCollection<string>? ccEmails = null, CancellationToken cancellationToken = default);
        Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
        Task SendTestEmailAsync(string toEmail, CancellationToken cancellationToken = default);
    }
}
