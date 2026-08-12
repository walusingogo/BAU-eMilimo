using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.Extensions.Logging;
using UCP.Core.Entities;
using UCP.Core.Services;
using UCP.Infrastructure.Data;

namespace UCP.API.Services;

/// <summary>
/// Centralized email notification service backed by SMTP. This is the single
/// place SMTP logic lives — controllers and business services call the
/// strongly-typed methods on <see cref="IEmailService"/> instead of building
/// MailMessage/SmtpClient instances themselves.
///
/// SMTP configuration is read from the database (System Settings screen) via
/// <see cref="ISmtpSettingsProvider"/> rather than appsettings.json, so an
/// administrator can change it at runtime without a redeploy.
///
/// Failures never propagate: a failed send is logged (DB + file, via
/// <see cref="ISystemLogWriter"/>, and via ILogger as a fallback) and
/// swallowed, so a notification problem never breaks the business operation
/// that triggered it. Every attempt — success or failure — is also recorded
/// in the email_logs table for the System Settings "Emails" screen. No SMTP
/// credentials or message bodies are ever placed in any log.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ISmtpSettingsProvider _settingsProvider;
    private readonly AppDbContext _context;
    private readonly ILogger<EmailService> _logger;
    private readonly ISystemLogWriter _logWriter;
    private readonly IWebHostEnvironment _environment;

    public EmailService(ISmtpSettingsProvider settingsProvider, AppDbContext context, ILogger<EmailService> logger, ISystemLogWriter logWriter, IWebHostEnvironment environment)
    {
        _settingsProvider = settingsProvider;
        _context = context;
        _logger = logger;
        _logWriter = logWriter;
        _environment = environment;
    }

    public Task SendPasswordResetAsync(string toEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default)
    {
        var body = BuildEmail(
            title: "Reset your password",
            greeting: $"Hi {recipientName},",
            bodyHtml: $"""
                <p>We received a request to reset your UCZ Trinity Congregation account password.</p>
                <p style="text-align:center;margin:28px 0;">
                    <a href="{WebUtility.HtmlEncode(resetLink)}" style="background:#1f3a5f;color:#ffffff;padding:12px 28px;border-radius:4px;text-decoration:none;font-weight:600;">Reset Password</a>
                </p>
                <p>This link expires in 1 hour. If you didn't request a password reset, you can safely ignore this email.</p>
                """);

        return SendAsync(toEmail, "Reset your UCZ Trinity Congregation password", body, cancellationToken, "PasswordReset");
    }

    public Task SendAccountActivatedAsync(string toEmail, string recipientName, CancellationToken cancellationToken = default)
    {
        var body = BuildEmail(
            title: "Your account is active",
            greeting: $"Hi {recipientName},",
            bodyHtml: "<p>Your UCZ Trinity Congregation account has been activated. You can now sign in.</p>");

        return SendAsync(toEmail, "Your UCZ Trinity Congregation account is active", body, cancellationToken, "AccountActivation");
    }

    public Task SendAccountDeactivatedAsync(string toEmail, string recipientName, CancellationToken cancellationToken = default)
    {
        var body = BuildEmail(
            title: "Your account has been deactivated",
            greeting: $"Hi {recipientName},",
            bodyHtml: "<p>Your UCZ Trinity Congregation account has been deactivated. If you believe this is a mistake, please contact your administrator.</p>");

        return SendAsync(toEmail, "Your UCZ Trinity Congregation account was deactivated", body, cancellationToken, "AccountDeactivation");
    }

    public Task SendRequisitionStatusChangedAsync(
        string toEmail,
        string recipientName,
        string requisitionNumber,
        string newStatus,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var body = BuildStatusEmail("Requisition update", $"Hi {recipientName},", "Requisition", requisitionNumber, newStatus, note);

        return SendAsync(toEmail, $"Requisition {requisitionNumber}: {FormatStatus(newStatus)}", body, cancellationToken, "RequisitionStatusChange");
    }

    public Task SendContributionStatusChangedAsync(
        string toEmail,
        string recipientName,
        string contributionNumber,
        string newStatus,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var body = BuildStatusEmail("Contribution update", $"Hi {recipientName},", "Contribution", contributionNumber, newStatus, note);

        return SendAsync(toEmail, $"Contribution {contributionNumber}: {FormatStatus(newStatus)}", body, cancellationToken, "ContributionStatusChange");
    }

    public Task SendAssetRequestStatusChangedAsync(string toEmail, string recipientName, string requestNumber,
        string assetName, int quantity, string newStatus, DateTime expectedOutAtUtc,
        DateTime expectedReturnAtUtc, bool paymentRequired, decimal? paymentAmount, string? note,
        CancellationToken cancellationToken = default)
    {
        var badgeColor = StatusBadgeColor(newStatus);
        var statusText = FormatStatus(newStatus);
        var safeNumber = WebUtility.HtmlEncode(requestNumber);
        var body = $"""
            <div style="margin:0 0 22px;padding:18px;background:#F8F1E4;border:1px solid #E8DDC8;border-radius:8px;">
                <div style="color:#8A7F6A;font-size:11px;font-weight:700;letter-spacing:.7px;text-transform:uppercase;">Asset request</div>
                <div style="margin-top:4px;color:#1f3a5f;font-size:20px;font-weight:750;">{safeNumber}</div>
            </div>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;margin-bottom:20px;">
                <tr><td style="padding:9px 0;color:#8A7F6A;width:42%;border-bottom:1px solid #EFE9DC;">Asset</td><td style="padding:9px 0;font-weight:650;border-bottom:1px solid #EFE9DC;">{WebUtility.HtmlEncode(assetName)}</td></tr>
                <tr><td style="padding:9px 0;color:#8A7F6A;border-bottom:1px solid #EFE9DC;">Quantity</td><td style="padding:9px 0;font-weight:650;border-bottom:1px solid #EFE9DC;">{quantity:N0}</td></tr>
                <tr><td style="padding:9px 0;color:#8A7F6A;border-bottom:1px solid #EFE9DC;">Expected checkout</td><td style="padding:9px 0;border-bottom:1px solid #EFE9DC;">{expectedOutAtUtc:dd MMM yyyy, HH:mm} CAT</td></tr>
                <tr><td style="padding:9px 0;color:#8A7F6A;border-bottom:1px solid #EFE9DC;">Expected return</td><td style="padding:9px 0;border-bottom:1px solid #EFE9DC;">{expectedReturnAtUtc:dd MMM yyyy, HH:mm} CAT</td></tr>
                <tr><td style="padding:9px 0;color:#8A7F6A;">Payment</td><td style="padding:9px 0;">{(paymentRequired ? $"ZMW {paymentAmount.GetValueOrDefault():N2}" : "Not required")}</td></tr>
            </table>
            <div style="margin:0 0 20px;">Status&nbsp;&nbsp;<span style="display:inline-block;background:{badgeColor};color:#fff;font-size:11px;font-weight:750;letter-spacing:.5px;text-transform:uppercase;padding:6px 13px;border-radius:999px;">{WebUtility.HtmlEncode(statusText)}</span></div>
            {(string.IsNullOrWhiteSpace(note) ? "" : $"""<div style="padding:13px 16px;background:#FAF7F0;border-left:3px solid #B8862E;border-radius:4px;"><div style="font-size:11px;color:#8A7F6A;text-transform:uppercase;font-weight:700;">Reviewer comment</div><div style="margin-top:4px;color:#4B4436;">{WebUtility.HtmlEncode(note)}</div></div>""")}
            """;
        var html = BuildEmail("Asset request update", $"Hi {recipientName},", body);
        return SendAsync(toEmail, $"Asset request {requestNumber}: {statusText}", html, cancellationToken, "AssetRequestStatusChange");
    }

    public Task SendApprovalActionRequiredAsync(string toEmail, string recipientName, string processName,
        string recordNumber, string summary, string approvalUrl, string[] actions,
        CancellationToken cancellationToken = default)
    {
        var actionList = string.Join(" &nbsp;•&nbsp; ", actions.Select(WebUtility.HtmlEncode));
        var body = $"""
            <div style="padding:18px;background:#F8F1E4;border:1px solid #E8DDC8;border-radius:8px;margin-bottom:20px;">
              <div style="font-size:11px;font-weight:700;letter-spacing:.7px;text-transform:uppercase;color:#8A7F6A;">{WebUtility.HtmlEncode(processName)}</div>
              <div style="font-size:20px;font-weight:750;color:#1F3A5F;margin-top:4px;">{WebUtility.HtmlEncode(recordNumber)}</div>
              <div style="font-size:13px;color:#4B4436;margin-top:8px;">{WebUtility.HtmlEncode(summary)}</div>
            </div>
            <p style="margin:0 0 12px;">This request requires your review. Available actions:</p>
            <p style="margin:0 0 24px;color:#8A651F;font-size:13px;font-weight:650;">{actionList}</p>
            <div style="text-align:center;margin:26px 0 18px;">
              <a href="{WebUtility.HtmlEncode(approvalUrl)}" style="display:inline-block;background:#1F3A5F;color:#fff;text-decoration:none;font-weight:700;padding:13px 26px;border-radius:7px;">Open request for review</a>
            </div>
            <p style="margin:0;color:#8A7F6A;font-size:12px;">For security, you may be asked to sign in. The system will return you directly to this request.</p>
            """;
        return SendAsync(toEmail, $"Approval required: {processName} {recordNumber}",
            BuildEmail("Approval required", $"Hi {recipientName},", body), cancellationToken, "ApprovalActionRequired");
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        => SendAsync(toEmail, subject, htmlBody, cancellationToken, "General");

    public Task SendTestEmailAsync(string toEmail, CancellationToken cancellationToken = default)
        => SendAsync(toEmail, "UCZ Trinity Congregation SMTP test",
            BuildEmail("SMTP test successful", "Hello,", "<p>Your database-managed email settings are working correctly.</p>"),
            cancellationToken, "TestEmail");

    // ============================================
    // INTERNAL SEND + TEMPLATE HELPERS
    // ============================================

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken, string action)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return;
        }

        var settings = await _settingsProvider.GetAsync(cancellationToken);

        if (!settings.Enabled)
        {
            _logger.LogInformation("Email notifications are disabled (SMTP settings: Enabled=false); skipping {Action} email.", action);
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(settings.FromEmail, settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            var logoPath = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "images", "ucz-logo.png");
            if (!File.Exists(logoPath))
            {
                logoPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", "frontend", "web", "public", "images", "ucz-logo.png"));
            }
            if (File.Exists(logoPath))
            {
                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
                var logo = new LinkedResource(logoPath, MediaTypeNames.Image.Png)
                {
                    ContentId = "ucz-logo",
                    TransferEncoding = TransferEncoding.Base64
                };
                htmlView.LinkedResources.Add(logo);
                message.AlternateViews.Add(htmlView);
            }

            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.EnableSsl,
                Timeout = settings.TimeoutMilliseconds,
                Credentials = string.IsNullOrWhiteSpace(settings.Username)
                    ? null
                    : new NetworkCredential(settings.Username, settings.Password)
            };

            await client.SendMailAsync(message, cancellationToken);

            await RecordEmailLogAsync(toEmail, subject, action, "Success", null);
        }
        catch (Exception ex)
        {
            // Never let an email failure break the calling business operation.
            // Never log the message body, credentials, or host/port details —
            // only enough to diagnose that a send failed and for what action.
            _logger.LogError(ex, "Failed to send {Action} email.", action);

            await RecordEmailLogAsync(toEmail, subject, action, "Failed", ex.Message);

            try
            {
                await _logWriter.WriteAsync(new SystemLog
                {
                    Level = "Error",
                    Category = "Email",
                    Process = "Notifications",
                    Action = action,
                    Message = $"Failed to send {action} notification email.",
                    Exception = ex.Message
                }, cancellationToken);
            }
            catch
            {
                // Logging must never throw back into the caller either.
            }
        }
    }

    private async Task RecordEmailLogAsync(string toEmail, string subject, string category, string status, string? failureReason)
    {
        try
        {
            _context.EmailLogs.Add(new EmailLog
            {
                ToEmail = toEmail,
                Subject = subject,
                Category = category,
                Status = status,
                FailureReason = failureReason
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // The email_logs write is best-effort bookkeeping; never let it
            // affect whether the caller considers the send successful.
            _logger.LogError(ex, "Failed to write email_logs entry for {Action}.", category);
        }
    }

    private static string FormatStatus(string status) =>
        string.Join(' ', status.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant() : w));

    private static string StatusBadgeColor(string status) => status.ToUpperInvariant() switch
    {
        "REJECTED" => "#B3261E",
        "APPROVED" or "CONFIRMED" or "ACTIVE" or "COLLECTED" or "COMPLETED" => "#2E7D32",
        "MONEY_READY" => "#B8862E",
        _ => "#1f3a5f"
    };

    /// <summary>
    /// Renders a status-change notification with a colored status chip, for
    /// requisitions and contributions.
    /// </summary>
    private static string BuildStatusEmail(string title, string greeting, string recordLabel, string recordNumber, string newStatus, string? note)
    {
        var badgeColor = StatusBadgeColor(newStatus);
        var statusText = FormatStatus(newStatus);

        var body = $"""
            <p style="margin:0 0 18px;">{recordLabel} <strong>{WebUtility.HtmlEncode(recordNumber)}</strong> status changed to:</p>
            <p style="margin:0 0 20px;">
                <span style="display:inline-block;background:{badgeColor};color:#ffffff;font-size:12px;font-weight:700;letter-spacing:.4px;text-transform:uppercase;padding:6px 14px;border-radius:999px;">{WebUtility.HtmlEncode(statusText)}</span>
            </p>
            {(string.IsNullOrWhiteSpace(note) ? "" : $"""<p style="margin:0;padding:12px 16px;background:#F8F1E4;border-left:3px solid #B8862E;border-radius:4px;color:#4b4436;">{WebUtility.HtmlEncode(note)}</p>""")}
            """;

        return BuildEmail(title, greeting, body);
    }

    private static string BuildEmail(string title, string greeting, string bodyHtml) => $"""
        <!DOCTYPE html>
        <html>
        <body style="margin:0;padding:0;background:#EEE9DD;font-family:'Segoe UI',Arial,sans-serif;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#EEE9DD;padding:36px 0;">
                <tr>
                    <td align="center">
                        <table role="presentation" width="520" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 1px 3px rgba(36,28,21,0.12);">
                            <tr>
                                <td style="height:4px;background:linear-gradient(90deg,#1f3a5f,#B8862E);"></td>
                            </tr>
                            <tr>
                                <td style="padding:28px 32px 20px;border-bottom:1px solid #EFE9DC;">
                                    <table role="presentation" cellpadding="0" cellspacing="0">
                                        <tr>
                                            <td style="width:42px;height:42px;text-align:center;vertical-align:middle;">
                                                <img src="cid:ucz-logo" width="40" height="40" alt="UCZ" style="display:block;width:40px;height:40px;object-fit:contain;border:0;" />
                                            </td>
                                            <td style="padding-left:12px;">
                                                <div style="color:#241C15;font-size:15px;font-weight:700;line-height:1.2;">UCZ Trinity Congregation</div>
                                                <div style="color:#8A7F6A;font-size:11px;letter-spacing:.3px;">Church Administration &amp; Financial Management</div>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding:32px;color:#241C15;font-size:14px;line-height:1.65;">
                                    <h2 style="margin:0 0 18px;font-size:19px;color:#1f3a5f;font-weight:700;">{WebUtility.HtmlEncode(title)}</h2>
                                    <p style="margin:0 0 14px;">{WebUtility.HtmlEncode(greeting)}</p>
                                    {bodyHtml}
                                </td>
                            </tr>
                            <tr>
                                <td style="padding:18px 32px;background:#FAF7F0;color:#8A7F6A;font-size:11.5px;line-height:1.6;border-top:1px solid #EFE9DC;">
                                    This is an automated message from the UCZ Trinity Congregation platform. Please do not reply to this email.
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
}
