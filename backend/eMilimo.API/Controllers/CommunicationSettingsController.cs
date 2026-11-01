using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eMilimo.API.Services;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/communication-settings"), Authorize(Roles = "Admin,SuperAdmin")]
public sealed class CommunicationSettingsController(ICommunicationSettingsProvider provider, ICommunicationService communication,
    AppDbContext db, IDataProtectionProvider protection) : ControllerBase
{
    private readonly IDataProtector _messageProtector = protection.CreateProtector("eMilimo.CommunicationLog.RetryPayload.v1");

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken token)
    {
        var settings = await provider.GetAsync(token);
        return Ok(new
        {
            providers = CommunicationProviders.Catalog,
            Sms = settings.Sms with { Secret = string.Empty },
            WhatsApp = settings.WhatsApp with { Secret = string.Empty },
            settings.TimeoutMilliseconds
        });
    }

    [HttpPut]
    public async Task<IActionResult> Save(CommunicationSettingsRequest request, CancellationToken token)
    {
        if (request.TimeoutMilliseconds is < 5000 or > 120000) return BadRequest(new { message = "Timeout must be between 5 and 120 seconds." });
        var existing = await provider.GetAsync(token);
        var smsError = CommunicationService.ValidateSettings(request.Sms, "SMS", existing.Sms.HasSecret && existing.Sms.Provider.Equals(request.Sms.Provider, StringComparison.OrdinalIgnoreCase));
        if (smsError is not null) return BadRequest(new { message = smsError });
        var whatsAppError = CommunicationService.ValidateSettings(request.WhatsApp, "WhatsApp", existing.WhatsApp.HasSecret && existing.WhatsApp.Provider.Equals(request.WhatsApp.Provider, StringComparison.OrdinalIgnoreCase));
        if (whatsAppError is not null) return BadRequest(new { message = whatsAppError });
        await provider.SaveAsync(new CommunicationSettings { Sms = request.Sms, WhatsApp = request.WhatsApp, TimeoutMilliseconds = request.TimeoutMilliseconds }, token);
        return Ok(new { message = "SMS and WhatsApp settings saved." });
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(TestCommunicationRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Recipient)) return BadRequest(new { message = "Enter a test recipient with country code." });
        try
        {
            if (request.Channel.Equals("sms", StringComparison.OrdinalIgnoreCase)) await communication.SendSmsAsync(request.Recipient, "eMilimo SMS integration test successful.", token, "TestSMS");
            else if (request.Channel.Equals("whatsapp", StringComparison.OrdinalIgnoreCase)) await communication.SendWhatsAppAsync(request.Recipient, "eMilimo WhatsApp integration test successful.", token, "TestWhatsApp");
            else return BadRequest(new { message = "Select SMS or WhatsApp." });
            return Ok(new { message = $"{request.Channel} test sent." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int limit = 100) => Ok(await db.CommunicationLogs.AsNoTracking().OrderByDescending(x => x.SentAtUtc).Take(Math.Clamp(limit, 1, 500)).Select(x => new { x.Id, x.SentAtUtc, x.Channel, x.Provider, x.Recipient, x.Action, x.Status, x.ProviderMessageId, x.FailureReason, x.RetryOfCommunicationLogId, CanRetry = x.Status == "Failed" && x.ProtectedMessage != null }).ToListAsync());

    [HttpPost("history/{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken token)
    {
        var original = await db.CommunicationLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, token);
        if (original is null) return NotFound(new { message = "Communication delivery record was not found." });
        if (original.Status != "Failed") return BadRequest(new { message = "Only failed SMS or WhatsApp deliveries can be retried." });
        if (string.IsNullOrWhiteSpace(original.ProtectedMessage)) return BadRequest(new { message = "This record does not contain a retryable message." });
        string message;
        try { message = _messageProtector.Unprotect(original.ProtectedMessage); }
        catch { return BadRequest(new { message = "The retained message can no longer be decrypted and cannot be retried." }); }
        try
        {
            if (original.Channel.Equals("SMS", StringComparison.OrdinalIgnoreCase)) await communication.SendSmsAsync(original.Recipient, message, token, original.Action, original.Id);
            else if (original.Channel.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase)) await communication.SendWhatsAppAsync(original.Recipient, message, token, original.Action, original.Id);
            else return BadRequest(new { message = "The communication channel is not supported." });
            return Ok(new { message = $"{original.Channel} retry sent successfully." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    public sealed record CommunicationSettingsRequest(CommunicationChannelSettings Sms, CommunicationChannelSettings WhatsApp, int TimeoutMilliseconds);
    public sealed record TestCommunicationRequest(string Channel, string Recipient);
}
