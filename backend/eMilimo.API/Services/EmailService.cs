using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Sockets;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Services;

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
    private readonly IEmailTemplateService _templates;

    public EmailService(ISmtpSettingsProvider settingsProvider, AppDbContext context, ILogger<EmailService> logger, ISystemLogWriter logWriter, IWebHostEnvironment environment, IEmailDeliveryQueue deliveryQueue, IDataProtectionProvider dataProtection, ICommunicationService communication, IEmailTemplateService templates)
    {
        _settingsProvider = settingsProvider;
        _context = context;
        _logger = logger;
        _logWriter = logWriter;
        _environment = environment;
        _deliveryQueue = deliveryQueue;
        _emailPayloadProtector = dataProtection.CreateProtector("eMilimo.EmailLog.RetryPayload.v1");
        _communication = communication;
        _templates = templates;
    }

    public async Task SendPasswordResetAsync(string toEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default)
    {
        await RenderAndSendAsync("password-reset", toEmail, new Dictionary<string, string?>
        {
            ["recipient_name"] = recipientName, ["reset_link"] = resetLink
        }, cancellationToken, "PasswordReset", $"Password reset requested. Use this secure link within 1 hour: {resetLink}");
    }

    public async Task SendTemporaryPasswordAsync(string toEmail, string recipientName, string temporaryPassword, CancellationToken cancellationToken = default)
    {
        await RenderAndSendAsync("temporary-password", toEmail, new Dictionary<string, string?>
        {
            ["recipient_name"] = recipientName, ["temporary_password"] = temporaryPassword
        }, cancellationToken, "TemporaryPassword", $"Your temporary eMilimo password is: {temporaryPassword}. Sign in and change it immediately. Do not share it.");
    }

    public async Task SendAccountActivatedAsync(string toEmail, string recipientName, CancellationToken cancellationToken = default)
    {
        await RenderAndSendAsync("account-activated", toEmail, new Dictionary<string, string?> { ["recipient_name"] = recipientName },
            cancellationToken, "AccountActivation", "Your eMilimo account has been activated. You can now sign in.");
    }

    public async Task SendAccountDeactivatedAsync(string toEmail, string recipientName, CancellationToken cancellationToken = default)
    {
        await RenderAndSendAsync("account-deactivated", toEmail, new Dictionary<string, string?> { ["recipient_name"] = recipientName },
            cancellationToken, "AccountDeactivation", "Your eMilimo account has been deactivated. Contact your administrator if you believe this is a mistake.");
    }

    public async Task SendRequisitionStatusChangedAsync(
        string toEmail,
        string recipientName,
        string requisitionNumber,
        string newStatus,
        string? note,
        CancellationToken cancellationToken = default)
    {
        await RenderAndSendAsync("requisition-status", toEmail, new Dictionary<string, string?>
        {
            ["recipient_name"] = recipientName, ["reference"] = requisitionNumber,
            ["status"] = FormatStatus(newStatus), ["note"] = note
        }, cancellationToken, "RequisitionStatusChange", $"Requisition {requisitionNumber} is now {FormatStatus(newStatus)}.{NoteText(note)}");
    }

    public async Task SendContributionStatusChangedAsync(
        string toEmail,
        string recipientName,
        string contributionNumber,
        string newStatus,
        string? note,
        CancellationToken cancellationToken = default)
    {
        await RenderAndSendAsync("contribution-status", toEmail, new Dictionary<string, string?>
        {
            ["recipient_name"] = recipientName, ["reference"] = contributionNumber,
            ["status"] = FormatStatus(newStatus), ["note"] = note
        }, cancellationToken, "ContributionStatusChange", $"Contribution {contributionNumber} is now {FormatStatus(newStatus)}.{NoteText(note)}");
    }

    public async Task SendAssetRequestStatusChangedAsync(string toEmail, string recipientName, string requestNumber,
        string assetName, int quantity, string newStatus, DateTime expectedOutAtUtc,
        DateTime expectedReturnAtUtc, bool paymentRequired, decimal? paymentAmount, string? note,
        CancellationToken cancellationToken = default)
    {
        var statusText = FormatStatus(newStatus);
        await RenderAndSendAsync("asset-request-status", toEmail, new Dictionary<string, string?>
        {
            ["recipient_name"] = recipientName, ["reference"] = requestNumber, ["status"] = statusText,
            ["asset_name"] = assetName, ["quantity"] = quantity.ToString("N0"),
            ["expected_out"] = $"{expectedOutAtUtc:dd MMM yyyy, HH:mm} CAT",
            ["expected_return"] = $"{expectedReturnAtUtc:dd MMM yyyy, HH:mm} CAT",
            ["payment"] = paymentRequired ? $"ZMW {paymentAmount.GetValueOrDefault():N2}" : "Not required",
            ["note"] = note
        }, cancellationToken, "AssetRequestStatusChange",
            $"Asset request {requestNumber} for {quantity:N0} x {assetName} is now {statusText}.{NoteText(note)}");
    }

    public async Task SendApprovalActionRequiredAsync(string toEmail, string recipientName, string processName,
        string recordNumber, string summary, string approvalUrl, string[] actions,
        IReadOnlyCollection<string>? ccEmails = null, CancellationToken cancellationToken = default)
    {
        await RenderAndSendAsync("approval-required", toEmail, new Dictionary<string, string?>
        {
            ["recipient_name"] = recipientName, ["process_name"] = processName, ["reference"] = recordNumber,
            ["summary"] = summary, ["actions"] = string.Join(" • ", actions), ["approval_url"] = approvalUrl
        }, cancellationToken, "ApprovalActionRequired",
            BuildApprovalCompanionMessage(processName, recordNumber, summary, approvalUrl, actions), ccEmails);
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

    public Task SendTemplateTestAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        => SendAsync(toEmail, subject, htmlBody, cancellationToken, "EmailTemplateTest", companionMessage: null);

    public Task SendTestEmailAsync(string toEmail, CancellationToken cancellationToken = default)
        => DeliverQueuedAsync(new QueuedEmail(toEmail, "[[CHURCH_NAME]] SMTP test",
            BuildEmail("SMTP test successful", "Hello,", "<p>Your database-managed email settings are working correctly.</p>"),
            "TestEmail", []), cancellationToken);

    // ============================================
    // INTERNAL SEND + TEMPLATE HELPERS
    // ============================================

    private async Task RenderAndSendAsync(string templateKey, string toEmail, IReadOnlyDictionary<string, string?> values,
        CancellationToken cancellationToken, string action, string? companionMessage,
        IReadOnlyCollection<string>? ccEmails = null)
    {
        try
        {
            var rendered = await _templates.RenderAsync(templateKey, values, cancellationToken);
            if (!rendered.IsEnabled)
            {
                _logger.LogInformation("Email template {TemplateKey} is disabled; skipping {Action} notification.", templateKey, action);
                await DeliverCompanionChannelsAsync(new QueuedEmail(toEmail, rendered.Subject, rendered.HtmlBody, action,
                    (ccEmails ?? Array.Empty<string>()).ToArray(),
                    SmsMessage: rendered.IsSmsEnabled ? rendered.SmsBody : null,
                    WhatsAppMessage: rendered.IsWhatsAppEnabled ? rendered.WhatsAppBody : null), cancellationToken);
                return;
            }

            await SendAsync(toEmail, rendered.Subject, rendered.HtmlBody, cancellationToken, action, ccEmails,
                companionMessage: null,
                smsMessage: rendered.IsSmsEnabled ? rendered.SmsBody : null,
                whatsAppMessage: rendered.IsWhatsAppEnabled ? rendered.WhatsAppBody : null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Notifications remain non-blocking: template lookup/rendering must
            // never roll back the workflow event that caused the notification.
            _logger.LogError(ex, "Could not render {TemplateKey} for {Action}.", templateKey, action);
            try
            {
                await _logWriter.WriteAsync(new SystemLog
                {
                    Level = "Error", Category = "Email", Process = "EmailTemplates", Action = action,
                    Message = $"Could not render email template {templateKey}.", Exception = ex.Message
                }, cancellationToken);
            }
            catch { }
        }
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken, string action,
        IReadOnlyCollection<string>? ccEmails = null, string? companionMessage = null,
        string? smsMessage = null, string? whatsAppMessage = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return;
        }

        var queued = _deliveryQueue.Enqueue(new QueuedEmail(toEmail, subject, htmlBody, action,
            (ccEmails ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            CompanionMessage: companionMessage, SmsMessage: smsMessage, WhatsAppMessage: whatsAppMessage));
        if (!queued)
        {
            _logger.LogWarning("The email delivery queue is full; {Action} email was not queued.", action);
            await RecordCommunicationLogAsync(toEmail, subject, action, "Failed", "The email delivery queue is full.", htmlBody, ccEmails);
        }
    }

    public async Task DeliverQueuedAsync(QueuedEmail queued, CancellationToken cancellationToken)
    {
        var toEmail = queued.ToEmail;
        var churchName = await _context.SystemConfigs.AsNoTracking()
            .Where(x => !x.IsArchived && x.ConfigKey == "church_name")
            .Select(x => x.ConfigValue).FirstOrDefaultAsync(cancellationToken) ?? "Church Administration";
        var subject = queued.Subject.Replace("[[CHURCH_NAME]]", churchName, StringComparison.Ordinal);
        var htmlBody = queued.HtmlBody.Replace("[[CHURCH_NAME]]", WebUtility.HtmlEncode(churchName), StringComparison.Ordinal);
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
                await RecordCommunicationLogAsync(toEmail, subject, action, "Failed", disabledReason, htmlBody, ccEmails, queued.RetryOfCommunicationLogId);
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

            await RecordCommunicationLogAsync(toEmail, subject, action, "Success", null, htmlBody, ccEmails, queued.RetryOfCommunicationLogId);
        }
        catch (Exception ex)
        {
            // Never let an email failure break the calling business operation.
            // Never log the message body, credentials, or host/port details —
            // only enough to diagnose that a send failed and for what action.
            _logger.LogError(ex, "Failed to send {Action} email.", action);

            var failureReason = BuildEmailFailureReason(ex, settings, elapsed.Elapsed);
            await RecordCommunicationLogAsync(toEmail, subject, action, "Failed", failureReason, htmlBody, ccEmails, queued.RetryOfCommunicationLogId);

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
            if ((!string.IsNullOrWhiteSpace(queued.CompanionMessage) || !string.IsNullOrWhiteSpace(queued.SmsMessage) || !string.IsNullOrWhiteSpace(queued.WhatsAppMessage)) && !queued.RetryOfCommunicationLogId.HasValue)
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
                var smsMessage = queued.SmsMessage ?? queued.CompanionMessage;
                var whatsAppMessage = queued.WhatsAppMessage ?? queued.CompanionMessage;
                if (!string.IsNullOrWhiteSpace(smsMessage))
                {
                    try { await _communication.SendSmsAsync(phone, smsMessage, cancellationToken, queued.Action); }
                    catch (Exception ex) { _logger.LogWarning(ex, "SMS companion delivery failed for {Action}.", queued.Action); }
                }
                if (!string.IsNullOrWhiteSpace(whatsAppMessage))
                {
                    try { await _communication.SendWhatsAppAsync(phone, whatsAppMessage, cancellationToken, queued.Action); }
                    catch (Exception ex) { _logger.LogWarning(ex, "WhatsApp companion delivery failed for {Action}.", queued.Action); }
                }
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

    private async Task RecordCommunicationLogAsync(string toEmail, string subject, string category, string status, string? failureReason,
        string? htmlBody = null, IReadOnlyCollection<string>? ccEmails = null, Guid? retryOfCommunicationLogId = null)
    {
        try
        {
            _context.CommunicationLogs.Add(new CommunicationLog
            {
                Channel = "Email",
                Provider = "SMTP",
                Recipient = toEmail,
                Subject = subject,
                Action = category,
                Status = status,
                FailureReason = failureReason,
                ProtectedMessage = string.IsNullOrWhiteSpace(htmlBody) ? null : _emailPayloadProtector.Protect(htmlBody),
                ProtectedCcRecipients = ccEmails == null ? null : _emailPayloadProtector.Protect(string.Join(';', ccEmails)),
                RetryOfCommunicationLogId = retryOfCommunicationLogId
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Delivery history is best-effort bookkeeping; never let it
            // affect whether the caller considers the send successful.
            _logger.LogError(ex, "Failed to write communication delivery entry for {Action}.", category);
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
                                                <div style="color:#241C15;font-size:15px;font-weight:700;line-height:1.2;">[[CHURCH_NAME]]</div>
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
                                    This is an automated message from the [[CHURCH_NAME]] platform. Please do not reply to this email.
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
