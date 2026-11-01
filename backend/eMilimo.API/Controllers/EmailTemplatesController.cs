using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eMilimo.API.Authorization;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/email-templates"), Authorize, RequirePermission("settings.manage")]
public sealed partial class EmailTemplatesController(
    AppDbContext db,
    IEmailTemplateService templates,
    IEmailService email,
    ICommunicationService communication,
    ICurrentUserService currentUser,
    ISystemLogWriter logs) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await db.EmailTemplates.AsNoTracking().ToDictionaryAsync(x => x.TemplateKey, StringComparer.OrdinalIgnoreCase, cancellationToken);
        return Ok(templates.Definitions.OrderBy(x => x.Name).Select(definition =>
        {
            rows.TryGetValue(definition.Key, out var row);
            return View(definition, row);
        }));
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, CancellationToken cancellationToken)
    {
        EmailTemplateDefinition definition;
        try { definition = templates.GetDefinition(key); }
        catch (KeyNotFoundException) { return NotFound(new { message = "Email template was not found." }); }
        var row = await db.EmailTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.TemplateKey == definition.Key, cancellationToken);
        return Ok(View(definition, row));
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Update(string key, UpdateEmailTemplateRequest request, CancellationToken cancellationToken)
    {
        EmailTemplateDefinition definition;
        try { definition = templates.GetDefinition(key); }
        catch (KeyNotFoundException) { return NotFound(new { message = "Email template was not found." }); }

        var validation = Validate(definition, request.Subject, request.HtmlBody, request.SmsBody, request.WhatsAppBody);
        if (validation is not null) return validation;

        var row = await db.EmailTemplates.SingleOrDefaultAsync(x => x.TemplateKey == definition.Key, cancellationToken);
        if (row is not null && request.Version != row.Version)
            return Conflict(new
            {
                message = "This email template changed while you were editing it. Reload it and try again.",
                currentVersion = row.Version
            });
        var before = row is null ? null : new { row.IsEnabled, row.Subject, row.HtmlBody, row.IsSmsEnabled, row.SmsBody, row.IsWhatsAppEnabled, row.WhatsAppBody, row.Version };
        if (row is null)
        {
            row = new EmailTemplate { Id = Guid.NewGuid(), TemplateKey = definition.Key, Name = definition.Name, Description = definition.Description, CreatedById = currentUser.GetUserId() };
            db.EmailTemplates.Add(row);
        }

        row.Name = definition.Name;
        row.Description = definition.Description;
        row.IsEnabled = request.IsEnabled;
        row.Subject = request.Subject.Trim();
        row.HtmlBody = request.HtmlBody.Trim();
        row.IsSmsEnabled = request.IsSmsEnabled;
        row.SmsBody = request.SmsBody.Trim();
        row.IsWhatsAppEnabled = request.IsWhatsAppEnabled;
        row.WhatsAppBody = request.WhatsAppBody.Trim();
        row.UpdatedById = currentUser.GetUserId();
        row.UpdatedDate = DateTime.UtcNow;
        if (row.Version < 1) row.Version = 1; else if (before is not null) row.Version++;
        await db.SaveChangesAsync(cancellationToken);

        await logs.WriteAsync(new SystemLog
        {
            Level = "Information", Category = "Audit", Process = "EmailTemplates", Action = "Updated",
            UserId = currentUser.GetUserId(), Message = $"Email template {definition.Key} was updated.",
            Details = JsonSerializer.Serialize(new { TemplateKey = definition.Key, Before = before, After = new { row.IsEnabled, row.Subject, row.HtmlBody, row.IsSmsEnabled, row.SmsBody, row.IsWhatsAppEnabled, row.WhatsAppBody, row.Version } })
        }, cancellationToken);
        return Ok(View(definition, row));
    }

    [HttpPost("{key}/preview")]
    public IActionResult Preview(string key, PreviewEmailTemplateRequest request)
    {
        EmailTemplateDefinition definition;
        try { definition = templates.GetDefinition(key); }
        catch (KeyNotFoundException) { return NotFound(new { message = "Email template was not found." }); }
        var validation = Validate(definition, request.Subject, request.HtmlBody, request.SmsBody, request.WhatsAppBody);
        if (validation is not null) return validation;
        var rendered = templates.Render(definition, request.IsEnabled, request.Subject, request.HtmlBody,
            request.IsSmsEnabled, request.SmsBody, request.IsWhatsAppEnabled, request.WhatsAppBody,
            definition.SampleValues.ToDictionary(x => x.Key, x => (string?)x.Value));
        return Ok(rendered);
    }

    [HttpPost("{key}/test")]
    public async Task<IActionResult> Test(string key, TestEmailTemplateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest(new { message = "A test recipient email address is required." });
        EmailTemplateDefinition definition;
        try { definition = templates.GetDefinition(key); }
        catch (KeyNotFoundException) { return NotFound(new { message = "Email template was not found." }); }
        var rendered = await templates.RenderAsync(definition.Key, definition.SampleValues.ToDictionary(x => x.Key, x => (string?)x.Value), cancellationToken);
        await email.SendTemplateTestAsync(request.Email.Trim(), "[TEST] " + rendered.Subject, rendered.HtmlBody, cancellationToken);
        return Accepted(new { message = "Template test email was queued." });
    }

    [HttpPost("{key}/test-channel")]
    public async Task<IActionResult> TestChannel(string key, TestChannelTemplateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Recipient)) return BadRequest(new { message = "A test phone number with country code is required." });
        EmailTemplateDefinition definition;
        try { definition = templates.GetDefinition(key); }
        catch (KeyNotFoundException) { return NotFound(new { message = "Notification template was not found." }); }
        var rendered = await templates.RenderAsync(definition.Key, definition.SampleValues.ToDictionary(x => x.Key, x => (string?)x.Value), cancellationToken);
        try
        {
            if (request.Channel.Equals("sms", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(rendered.SmsBody)) return BadRequest(new { message = "This notification does not have an SMS template." });
                await communication.SendSmsAsync(request.Recipient.Trim(), rendered.SmsBody, cancellationToken, "NotificationTemplateTest");
            }
            else if (request.Channel.Equals("whatsapp", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(rendered.WhatsAppBody)) return BadRequest(new { message = "This notification does not have a WhatsApp template." });
                await communication.SendWhatsAppAsync(request.Recipient.Trim(), rendered.WhatsAppBody, cancellationToken, "NotificationTemplateTest");
            }
            else return BadRequest(new { message = "Select SMS or WhatsApp." });
            return Ok(new { message = $"{request.Channel} template test sent." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    private BadRequestObjectResult? Validate(EmailTemplateDefinition definition, string subject, string htmlBody, string smsBody, string whatsAppBody)
    {
        if (string.IsNullOrWhiteSpace(subject)) return BadRequest(new { message = "An email subject is required." });
        if (subject.Length > 255) return BadRequest(new { message = "The email subject cannot exceed 255 characters." });
        if (string.IsNullOrWhiteSpace(htmlBody)) return BadRequest(new { message = "Email HTML content is required." });
        if (htmlBody.Length > 200_000) return BadRequest(new { message = "Email HTML content cannot exceed 200,000 characters." });
        if (DangerousHtmlRegex().IsMatch(htmlBody)) return BadRequest(new { message = "Scripts, frames, embedded objects and inline event handlers are not allowed in email templates." });
        if (smsBody.Length > 1600) return BadRequest(new { message = "SMS content cannot exceed 1,600 characters." });
        if (whatsAppBody.Length > 4096) return BadRequest(new { message = "WhatsApp content cannot exceed 4,096 characters." });
        var unknown = templates.FindUnknownPlaceholders(definition, subject, htmlBody, smsBody, whatsAppBody);
        return unknown.Count > 0 ? BadRequest(new { message = $"Unknown placeholder(s): {string.Join(", ", unknown.Select(x => "{{" + x + "}}"))}." }) : null;
    }

    private static object View(EmailTemplateDefinition definition, EmailTemplate? row) => new
    {
        definition.Key, definition.Name, definition.Description,
        IsEnabled = row?.IsEnabled ?? true,
        Subject = row?.Subject ?? definition.DefaultSubject,
        HtmlBody = row?.HtmlBody ?? definition.DefaultHtmlBody,
        IsSmsEnabled = row?.IsSmsEnabled ?? definition.DefaultSmsEnabled,
        SmsBody = row?.SmsBody ?? definition.DefaultSmsBody,
        IsWhatsAppEnabled = row?.IsWhatsAppEnabled ?? definition.DefaultWhatsAppEnabled,
        WhatsAppBody = row?.WhatsAppBody ?? definition.DefaultWhatsAppBody,
        Placeholders = definition.Placeholders.Select(x => new { Key = x, Token = "{{" + x + "}}", Sample = definition.SampleValues.GetValueOrDefault(x, string.Empty) }),
        Version = row?.Version ?? 0,
        IsCustomized = row is not null,
        UpdatedDate = row?.UpdatedDate
    };

    [GeneratedRegex(@"<\s*(script|iframe|object|embed|form|base)\b|\son[a-z]+\s*=|javascript\s*:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DangerousHtmlRegex();

    public sealed record UpdateEmailTemplateRequest(bool IsEnabled, string Subject, string HtmlBody,
        bool IsSmsEnabled, string SmsBody, bool IsWhatsAppEnabled, string WhatsAppBody, int Version);
    public sealed record PreviewEmailTemplateRequest(bool IsEnabled, string Subject, string HtmlBody,
        bool IsSmsEnabled, string SmsBody, bool IsWhatsAppEnabled, string WhatsAppBody);
    public sealed record TestEmailTemplateRequest(string Email);
    public sealed record TestChannelTemplateRequest(string Channel, string Recipient);
}
