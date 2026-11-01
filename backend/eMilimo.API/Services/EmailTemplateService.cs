using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Services;

public sealed record RenderedEmailTemplate(
    bool IsEnabled, string Subject, string HtmlBody,
    bool IsSmsEnabled, string SmsBody,
    bool IsWhatsAppEnabled, string WhatsAppBody);

public sealed record EmailTemplateDefinition(
    string Key,
    string Name,
    string Description,
    string DefaultSubject,
    string DefaultHtmlBody,
    bool DefaultSmsEnabled,
    string DefaultSmsBody,
    bool DefaultWhatsAppEnabled,
    string DefaultWhatsAppBody,
    IReadOnlyList<string> Placeholders,
    IReadOnlyDictionary<string, string> SampleValues);

public interface IEmailTemplateService
{
    IReadOnlyCollection<EmailTemplateDefinition> Definitions { get; }
    EmailTemplateDefinition GetDefinition(string key);
    Task<RenderedEmailTemplate> RenderAsync(string key, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken = default);
    RenderedEmailTemplate Render(EmailTemplateDefinition definition, bool enabled, string subject, string htmlBody,
        bool smsEnabled, string smsBody, bool whatsAppEnabled, string whatsAppBody,
        IReadOnlyDictionary<string, string?> values);
    IReadOnlyCollection<string> FindUnknownPlaceholders(EmailTemplateDefinition definition, params string[] content);
}

public sealed partial class EmailTemplateService(AppDbContext db) : IEmailTemplateService
{
    private static readonly IReadOnlyDictionary<string, EmailTemplateDefinition> Catalog = BuildCatalog()
        .ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<EmailTemplateDefinition> Definitions => Catalog.Values.ToArray();

    public EmailTemplateDefinition GetDefinition(string key) => Catalog.TryGetValue(key, out var definition)
        ? definition
        : throw new KeyNotFoundException($"Email template '{key}' is not registered.");

    public async Task<RenderedEmailTemplate> RenderAsync(string key, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken = default)
    {
        var definition = GetDefinition(key);
        var stored = await db.EmailTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.TemplateKey == definition.Key, cancellationToken);
        var expandedValues = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);
        if (!expandedValues.ContainsKey("church_name"))
            expandedValues["church_name"] = await db.SystemConfigs.AsNoTracking()
                .Where(x => !x.IsArchived && x.ConfigKey == "church_name")
                .Select(x => x.ConfigValue).FirstOrDefaultAsync(cancellationToken) ?? "Church Administration";
        return Render(definition, stored?.IsEnabled ?? true, stored?.Subject ?? definition.DefaultSubject,
            stored?.HtmlBody ?? definition.DefaultHtmlBody,
            stored?.IsSmsEnabled ?? definition.DefaultSmsEnabled, stored?.SmsBody ?? definition.DefaultSmsBody,
            stored?.IsWhatsAppEnabled ?? definition.DefaultWhatsAppEnabled, stored?.WhatsAppBody ?? definition.DefaultWhatsAppBody,
            expandedValues);
    }

    public RenderedEmailTemplate Render(EmailTemplateDefinition definition, bool enabled, string subject, string htmlBody,
        bool smsEnabled, string smsBody, bool whatsAppEnabled, string whatsAppBody,
        IReadOnlyDictionary<string, string?> values)
    {
        var normalized = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);
        string Expand(string input, bool encode)
        {
            return PlaceholderRegex().Replace(input, match =>
            {
                var key = match.Groups[1].Value;
                var value = normalized.GetValueOrDefault(key) ?? string.Empty;
                return encode ? WebUtility.HtmlEncode(value) : value;
            });
        }

        return new RenderedEmailTemplate(enabled, Expand(subject, false), Expand(htmlBody, true),
            smsEnabled, Expand(smsBody, false), whatsAppEnabled, Expand(whatsAppBody, false));
    }

    public IReadOnlyCollection<string> FindUnknownPlaceholders(EmailTemplateDefinition definition, params string[] content)
    {
        var allowed = definition.Placeholders.Append("church_name").ToHashSet(StringComparer.OrdinalIgnoreCase);
        return PlaceholderRegex().Matches(string.Join('\n', content))
            .Select(x => x.Groups[1].Value)
            .Where(x => !allowed.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();
    }

    [GeneratedRegex(@"\{\{\s*([a-zA-Z][a-zA-Z0-9_]*)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    private static IReadOnlyCollection<EmailTemplateDefinition> BuildCatalog()
    {
        var commonSamples = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["church_name"] = "Trinity Congregation",
            ["recipient_name"] = "Jane Banda",
            ["reference"] = "REF-2026-0042",
            ["status"] = "Approved",
            ["note"] = "Reviewed and approved.",
            ["reset_link"] = "https://emilimo.example/reset/sample",
            ["temporary_password"] = "Temp#2026!",
            ["asset_name"] = "Church bus",
            ["quantity"] = "1",
            ["expected_out"] = "20 Oct 2026, 08:00 CAT",
            ["expected_return"] = "20 Oct 2026, 17:00 CAT",
            ["payment"] = "ZMW 1,500.00",
            ["process_name"] = "Requisition",
            ["summary"] = "Purchase of worship supplies",
            ["actions"] = "Approve • Reject • Return",
            ["approval_url"] = "https://emilimo.example/requisitions/sample"
        };

        EmailTemplateDefinition Define(string key, string name, string description, string subject, string title,
            string content, params string[] placeholders)
        {
            var all = placeholders.Append("church_name").Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var samples = all.ToDictionary(x => x, x => commonSamples.GetValueOrDefault(x, $"Sample {x.Replace('_', ' ')}"), StringComparer.OrdinalIgnoreCase);
            var companion = DefaultCompanion(key);
            var companionEnabled = !string.IsNullOrWhiteSpace(companion);
            return new EmailTemplateDefinition(key, name, description, subject, Shell(title, content),
                companionEnabled, companion, companionEnabled, companion, all, samples);
        }

        return
        [
            Define("password-reset", "Password reset", "Sent when a user requests a password reset.",
                "Reset your {{church_name}} password", "Reset your password",
                "<p>Hi {{recipient_name}},</p><p>We received a request to reset your {{church_name}} account password.</p><p style='text-align:center;margin:28px 0'><a href='{{reset_link}}' style='background:#1f3a5f;color:#fff;padding:12px 28px;border-radius:4px;text-decoration:none;font-weight:600'>Reset Password</a></p><p>This link expires in 1 hour. If you did not request it, you can ignore this email.</p>",
                "recipient_name", "reset_link"),
            Define("temporary-password", "Temporary password", "Sent after an administrator resets a user's password.",
                "Your temporary account password", "Your temporary password",
                "<p>Hi {{recipient_name}},</p><p>An administrator reset your account password.</p><div style='margin:24px 0;padding:16px;background:#F3F5F7;border:1px solid #D9DEE5;border-radius:8px;text-align:center'><div style='font-size:12px;color:#64748B;margin-bottom:6px'>TEMPORARY PASSWORD</div><strong style='font-family:monospace;font-size:20px;letter-spacing:2px'>{{temporary_password}}</strong></div><p>Please sign in and change it immediately. Do not share it.</p>",
                "recipient_name", "temporary_password"),
            Define("account-activated", "Account activated", "Sent when a user account is activated.",
                "Your {{church_name}} account is active", "Your account is active",
                "<p>Hi {{recipient_name}},</p><p>Your {{church_name}} account has been activated. You can now sign in.</p>", "recipient_name"),
            Define("account-deactivated", "Account deactivated", "Sent when a user account is deactivated.",
                "Your {{church_name}} account was deactivated", "Your account has been deactivated",
                "<p>Hi {{recipient_name}},</p><p>Your {{church_name}} account has been deactivated. If you believe this is a mistake, please contact your administrator.</p>", "recipient_name"),
            Define("requisition-status", "Requisition status", "Sent when a requisition changes status.",
                "Requisition {{reference}}: {{status}}", "Requisition update",
                StatusContent("Requisition"), "recipient_name", "reference", "status", "note"),
            Define("contribution-status", "Contribution status", "Sent when a contribution changes status.",
                "Contribution {{reference}}: {{status}}", "Contribution update",
                StatusContent("Contribution"), "recipient_name", "reference", "status", "note"),
            Define("asset-request-status", "Asset request status", "Sent when an asset request changes status.",
                "Asset request {{reference}}: {{status}}", "Asset request update",
                "<p>Hi {{recipient_name}},</p><p><strong>{{reference}}</strong> is now <strong>{{status}}</strong>.</p><table role='presentation' width='100%' style='border-collapse:collapse'><tr><td style='padding:8px 0;color:#8A7F6A'>Asset</td><td>{{asset_name}}</td></tr><tr><td style='padding:8px 0;color:#8A7F6A'>Quantity</td><td>{{quantity}}</td></tr><tr><td style='padding:8px 0;color:#8A7F6A'>Expected checkout</td><td>{{expected_out}}</td></tr><tr><td style='padding:8px 0;color:#8A7F6A'>Expected return</td><td>{{expected_return}}</td></tr><tr><td style='padding:8px 0;color:#8A7F6A'>Payment</td><td>{{payment}}</td></tr></table><p>{{note}}</p>",
                "recipient_name", "reference", "status", "asset_name", "quantity", "expected_out", "expected_return", "payment", "note"),
            Define("approval-required", "Approval required", "Sent to an authorised checker when workflow action is required.",
                "Approval required: {{process_name}} {{reference}}", "Approval required",
                "<p>Hi {{recipient_name}},</p><div style='padding:18px;background:#F8F1E4;border:1px solid #E8DDC8;border-radius:8px'><strong>{{process_name}} {{reference}}</strong><p>{{summary}}</p></div><p>Available actions: <strong>{{actions}}</strong></p><p style='text-align:center;margin:26px 0'><a href='{{approval_url}}' style='display:inline-block;background:#1F3A5F;color:#fff;text-decoration:none;font-weight:700;padding:13px 26px;border-radius:7px'>Open request for review</a></p>",
                "recipient_name", "process_name", "reference", "summary", "actions", "approval_url")
        ];
    }

    private static string StatusContent(string label) =>
        $"<p>Hi {{{{recipient_name}}}},</p><p>{label} <strong>{{{{reference}}}}</strong> is now <strong>{{{{status}}}}</strong>.</p><p>{{{{note}}}}</p>";

    private static string DefaultCompanion(string key) => key switch
    {
        // Credential and password-recovery notifications remain email-only by default.
        "password-reset" or "temporary-password" => string.Empty,
        "account-activated" => "Hi {{recipient_name}}, your {{church_name}} account has been activated. You can now sign in.",
        "account-deactivated" => "Hi {{recipient_name}}, your {{church_name}} account has been deactivated. Contact your administrator if this is unexpected.",
        "requisition-status" => "Requisition {{reference}} is now {{status}}. {{note}}",
        "contribution-status" => "Contribution {{reference}} is now {{status}}. {{note}}",
        "asset-request-status" => "Asset request {{reference}} for {{quantity}} x {{asset_name}} is now {{status}}. {{note}}",
        "approval-required" => "Approval required\n\n{{process_name}}: {{reference}}\n{{summary}}\n\nAvailable actions: {{actions}}\n\nOpen request:\n{{approval_url}}",
        _ => "{{church_name}} notification"
    };

    private static string Shell(string title, string content) => """
        <!DOCTYPE html><html><body style="margin:0;padding:0;background:#EEE9DD;font-family:'Segoe UI',Arial,sans-serif">
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#EEE9DD;padding:36px 0"><tr><td align="center">
        <table role="presentation" width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;overflow:hidden">
        <tr><td style="height:4px;background:#B8862E"></td></tr>
        <tr><td style="padding:24px 32px;border-bottom:1px solid #EFE9DC"><table role="presentation"><tr><td><img src="cid:ucz-logo" width="40" height="40" alt="UCZ" style="display:block;object-fit:contain"></td><td style="padding-left:12px"><strong style="color:#241C15">{{church_name}}</strong><div style="color:#8A7F6A;font-size:11px">Church Administration &amp; Financial Management</div></td></tr></table></td></tr>
        <tr><td style="padding:32px;color:#241C15;font-size:14px;line-height:1.65"><h2 style="margin:0 0 18px;font-size:19px;color:#1f3a5f">__TITLE__</h2>__CONTENT__</td></tr>
        <tr><td style="padding:18px 32px;background:#FAF7F0;color:#8A7F6A;font-size:11.5px;border-top:1px solid #EFE9DC">This is an automated message from {{church_name}}. Please do not reply.</td></tr>
        </table></td></tr></table></body></html>
        """.Replace("__TITLE__", title, StringComparison.Ordinal).Replace("__CONTENT__", content, StringComparison.Ordinal);
}
