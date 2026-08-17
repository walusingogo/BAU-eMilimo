using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Sockets;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
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
    private readonly IEmailDeliveryQueue _deliveryQueue;
    private readonly IDataProtector _emailPayloadProtector;
    private readonly ICommunicationService _communication;

    public EmailService(ISmtpSettingsProvider settingsProvider, AppDbContext context, ILogger<EmailService> logger, ISystemLogWriter logWriter, IWebHostEnvironment environment, IEmailDeliveryQueue deliveryQueue, IDataProtectionProvider dataProtection, ICommunicationService communication)
    {
        _settingsProvider = settingsProvider;
        _context = context;
        _logger = logger;
        _logWriter = logWriter;
        _environment = environment;
        _deliveryQueue = deliveryQueue;
        _emailPayloadProtector = dataProtection.CreateProtector("UCP.EmailLog.RetryPayload.v1");
        _communication = communication;
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

        return SendAsync(toEmail, "Reset your UCZ Trinity Congregation password", body, cancellationToken, "PasswordReset",
            companionMessage: $"Password reset requested. Use this secure link within 1 hour: {resetLink}");
    }

    public Task SendTemporaryPasswordAsync(string toEmail, string recipientName, string temporaryPassword, CancellationToken cancellationToken = default)
    {
        var body = BuildEmail(
            title: "Your temporary password",
            greeting: $"Hi {recipientName},",
            bodyHtml: $"""
                <p>An administrator reset your account password.</p>
                <div style="margin:24px 0;padding:16px;background:#F3F5F7;border:1px solid #D9DEE5;border-radius:8px;text-align:center;">
                    <div style="font-size:12px;color:#64748B;margin-bottom:6px;">TEMPORARY PASSWORD</div>
                    <strong style="font-family:monospace;font-size:20px;letter-spacing:2px;color:#241C15;">{WebUtility.HtmlEncode(temporaryPassword)}</strong>
                </div>
                <p>Please sign in and change this temporary password immediately. Do not share it with anyone.</p>
                """);
        return SendAsync(toEmail, "Your temporary account password", body, cancellationToken, "TemporaryPassword",
            companionMessage: $"Your temporary UCP password is: {temporaryPassword}. Sign in and change it immediately. Do not share it.");
    }

    public Task SendAccountActivatedAsync(string toEmail, string recipientName, CancellationToken cancellationToken = default)
    {
        var body = BuildEmail(
            title: "Your account is active",
            greeting: $"Hi {recipientName},",
            bodyHtml: "<p>Your UCZ Trinity Congregation account has been activated. You can now sign in.</p>");

        return SendAsync(toEmail, "Your UCZ Trinity Congregation account is active", body, cancellationToken, "AccountActivation",
            companionMessage: "Your UCP account has been activated. You can now sign in.");
    }

    public Task SendAccountDeactivatedAsync(string toEmail, string recipientName, CancellationToken cancellationToken = default)
    {
        var body = BuildEmail(
            title: "Your account has been deactivated",
            greeting: $"Hi {recipientName},",
            bodyHtml: "<p>Your UCZ Trinity Congregation account has been deactivated. If you believe this is a mistake, please contact your administrator.</p>");

        return SendAsync(toEmail, "Your UCZ Trinity Congregation account was deactivated", body, cancellationToken, "AccountDeactivation",
            companionMessage: "Your UCP account has been deactivated. Contact your administrator if you believe this is a mistake.");
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

        return SendAsync(toEmail, $"Requisition {requisitionNumber}: {FormatStatus(newStatus)}", body, cancellationToken, "RequisitionStatusChange",
            companionMessage: $"Requisition {requisitionNumber} is now {FormatStatus(newStatus)}.{NoteText(note)}");
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

        return SendAsync(toEmail, $"Contribution {contributionNumber}: {FormatStatus(newStatus)}", body, cancellationToken, "ContributionStatusChange",
            companionMessage: $"Contribution {contributionNumber} is now {FormatStatus(newStatus)}.{NoteText(note)}");
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
        return SendAsync(toEmail, $"Asset request {requestNumber}: {statusText}", html, cancellationToken, "AssetRequestStatusChange",
            companionMessage: $"Asset request {requestNumber} for {quantity:N0} x {assetName} is now {statusText}.{NoteText(note)}");
    }

    public Task SendApprovalActionRequiredAsync(string toEmail, string recipientName, string processName,
        string recordNumber, string summary, string approvalUrl, string[] actions,
        IReadOnlyCollection<string>? ccEmails = null, CancellationToken cancellationToken = default)
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
            BuildEmail("Approval required", $"Hi {recipientName},", body), cancellationToken, "ApprovalActionRequired", ccEmails,
            BuildApprovalCompanionMessage(processName, recordNumber, summary, approvalUrl, actions));
    }

    private static string BuildApprovalCompanionMessage(string processName, string recordNumber, string summary,
        string approvalUrl, IReadOnlyCollection<string> actions)
    {
        // Keep the URL complete and on its own line. WhatsApp and SMS clients are much
        // more reliable at linkifying an absolute URL when punctuation/text does not
        // immediately surround it.
        var url = approvalUrl.Trim();
        var availableActions = string.Join(", ", actions.Where(x => !string.IsNullOrWhiteSpace(x)));
        return $"Approval required\n\n{processName}: {recordNumber}\n{summary}\n\nAvailable actions: {availableActions}\n\nOpen request for review:\n{url}";
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        => SendAsync(toEmail, subject, htmlBody, cancellationToken, "General", companionMessage: subject);

    public Task SendTestEmailAsync(string toEmail, CancellationToken cancellationToken = default)
        => DeliverQueuedAsync(new QueuedEmail(toEmail, "UCZ Trinity Congregation SMTP test",
            BuildEmail("SMTP test successful", "Hello,", "<p>Your database-managed email settings are working correctly.</p>"),
            "TestEmail", []), cancellationToken);

    // ============================================
    // INTERNAL SEND + TEMPLATE HELPERS
    // ============================================

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken, string action,
        IReadOnlyCollection<string>? ccEmails = null, string? companionMessage = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return;
        }

        var queued = _deliveryQueue.Enqueue(new QueuedEmail(toEmail, subject, htmlBody, action,
            (ccEmails ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            CompanionMessage: companionMessage));
        if (!queued)
        {
            _logger.LogWarning("The email delivery queue is full; {Action} email was not queued.", action);
            await RecordEmailLogAsync(toEmail, subject, action, "Failed", "The email delivery queue is full.", htmlBody, ccEmails);
        }
    }

    public async Task DeliverQueuedAsync(QueuedEmail queued, CancellationToken cancellationToken)
    {
        var toEmail = queued.ToEmail;
        var subject = queued.Subject;
        var htmlBody = queued.HtmlBody;
        var action = queued.Action;
        var ccEmails = queued.CcEmails;

        SmtpSetting? settings = null;
        var elapsed = Stopwatch.StartNew();

        try
        {
            settings = await _settingsProvider.GetAsync(cancellationToken);

            if (!settings.Enabled)
            {
                const string disabledReason = "Delivery was not attempted because SMTP notifications are disabled in System Settings.";
                _logger.LogInformation("Email notifications are disabled (SMTP settings: Enabled=false); skipping {Action} email.", action);
                await RecordEmailLogAsync(toEmail, subject, action, "Failed", disabledReason, htmlBody, ccEmails, queued.RetryOfEmailLogId);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(settings.FromEmail, settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);
            foreach (var cc in (ccEmails ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals(toEmail, StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase))
                message.CC.Add(cc);

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

            using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCancellation.CancelAfter(settings.TimeoutMilliseconds);
            await client.SendMailAsync(message, timeoutCancellation.Token);

            await RecordEmailLogAsync(toEmail, subject, action, "Success", null, htmlBody, ccEmails, queued.RetryOfEmailLogId);
        }
        catch (Exception ex)
        {
            // Never let an email failure break the calling business operation.
            // Never log the message body, credentials, or host/port details —
            // only enough to diagnose that a send failed and for what action.
            _logger.LogError(ex, "Failed to send {Action} email.", action);

            var failureReason = BuildEmailFailureReason(ex, settings, elapsed.Elapsed);
            await RecordEmailLogAsync(toEmail, subject, action, "Failed", failureReason, htmlBody, ccEmails, queued.RetryOfEmailLogId);

            try
            {
                await _logWriter.WriteAsync(new SystemLog
                {
                    Level = "Error",
                    Category = "Email",
                    Process = "Notifications",
                    Action = action,
                    Message = $"Failed to send {action} notification email.",
                    Exception = failureReason
                }, cancellationToken);
            }
            catch
            {
                // Logging must never throw back into the caller either.
            }
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(queued.CompanionMessage) && !queued.RetryOfEmailLogId.HasValue)
                await DeliverCompanionChannelsAsync(queued, cancellationToken);
        }
    }

    private async Task DeliverCompanionChannelsAsync(QueuedEmail queued, CancellationToken cancellationToken)
    {
        try
        {
            var emails = queued.CcEmails.Append(queued.ToEmail).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var phones = await _context.Users.AsNoTracking()
                .Where(x => emails.Contains(x.Email) && x.PhoneNumber != null && x.PhoneNumber != "")
                .Select(x => x.PhoneNumber!).Distinct().ToListAsync(cancellationToken);
            foreach (var phone in phones)
            {
                try { await _communication.SendSmsAsync(phone, queued.CompanionMessage!, cancellationToken, queued.Action); }
                catch (Exception ex) { _logger.LogWarning(ex, "SMS companion delivery failed for {Action}.", queued.Action); }
                try { await _communication.SendWhatsAppAsync(phone, queued.CompanionMessage!, cancellationToken, queued.Action); }
                catch (Exception ex) { _logger.LogWarning(ex, "WhatsApp companion delivery failed for {Action}.", queued.Action); }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not resolve companion notification recipients for {Action}.", queued.Action);
        }
    }

    private static string BuildEmailFailureReason(Exception exception, SmtpSetting? settings, TimeSpan elapsed)
    {
        var parts = new List<string> { "SMTP delivery failed" };

        if (settings is not null)
        {
            parts.Add($"endpoint={settings.Host}:{settings.Port}");
            parts.Add($"TLS={(settings.EnableSsl ? "enabled" : "disabled")}");
            parts.Add($"timeout={settings.TimeoutMilliseconds}ms");
        }
        else
        {
            parts.Add("SMTP settings could not be loaded");
        }

        parts.Add($"elapsed={Math.Max(0, (long)elapsed.TotalMilliseconds)}ms");

        var exceptions = new List<Exception>();
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is AggregateException aggregate)
                exceptions.AddRange(aggregate.Flatten().InnerExceptions);
            else
                exceptions.Add(current);
        }

        var smtpException = exceptions.OfType<SmtpException>().FirstOrDefault();
        if (smtpException is not null)
            parts.Add($"SMTP status={smtpException.StatusCode}");

        var socketException = exceptions.OfType<SocketException>().FirstOrDefault();
        if (socketException is not null)
        {
            var socketSummary = socketException.SocketErrorCode switch
            {
                SocketError.TimedOut => "SMTP server did not respond before the connection timed out",
                SocketError.ConnectionRefused => "SMTP server refused the connection",
                SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain => "SMTP hostname could not be resolved",
                SocketError.NetworkUnreachable or SocketError.HostUnreachable => "SMTP server is unreachable from this machine",
                SocketError.ConnectionReset or SocketError.ConnectionAborted => "SMTP connection was closed unexpectedly",
                _ => "SMTP network connection failed"
            };
            parts.Add($"socket={socketException.SocketErrorCode} ({socketException.NativeErrorCode}): {socketSummary}");
        }

        if (exceptions.Any(x => x is OperationCanceledException) && settings is not null)
            parts.Add($"cause=SMTP operation exceeded the configured {settings.TimeoutMilliseconds / 1000d:0.#}-second timeout");

        var messages = exceptions
            .Select(x => CollapseWhitespace(x.Message))
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals("Failure sending mail.", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();
        if (messages.Length > 0)
            parts.Add($"cause={string.Join(" -> ", messages)}");

        var result = string.Join("; ", parts) + ".";
        return result.Length <= 1000 ? result : result[..997] + "...";
    }

    private static string CollapseWhitespace(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private async Task RecordEmailLogAsync(string toEmail, string subject, string category, string status, string? failureReason,
        string? htmlBody = null, IReadOnlyCollection<string>? ccEmails = null, Guid? retryOfEmailLogId = null)
    {
        try
        {
            _context.EmailLogs.Add(new EmailLog
            {
                ToEmail = toEmail,
                Subject = subject,
                Category = category,
                Status = status,
                FailureReason = failureReason,
                HtmlBody = string.IsNullOrWhiteSpace(htmlBody) ? null : _emailPayloadProtector.Protect(htmlBody),
                CcEmails = ccEmails == null ? null : _emailPayloadProtector.Protect(string.Join(';', ccEmails)),
                RetryOfEmailLogId = retryOfEmailLogId
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

    private static string NoteText(string? note) => string.IsNullOrWhiteSpace(note) ? "" : $" Comment: {note.Trim()}";

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
