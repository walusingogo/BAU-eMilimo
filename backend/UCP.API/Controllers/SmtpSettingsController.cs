using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCP.API.Services;
using UCP.Core.Entities;
using UCP.Core.Services;
using UCP.Infrastructure.Data;

namespace UCP.API.Controllers;

[ApiController, Route("api/smtp-settings"), Authorize(Roles = "SuperAdmin,Admin")]
public class SmtpSettingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ISmtpSettingsProvider _provider;
    private readonly IEmailService _email;

    public SmtpSettingsController(AppDbContext context, ISmtpSettingsProvider provider, IEmailService email)
        => (_context, _provider, _email) = (context, provider, email);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var row = await _context.SmtpSettings.AsNoTracking().FirstOrDefaultAsync();
        return Ok(row == null ? new { enabled=false, host="", port=587, enableSsl=true, username="", hasPassword=false, fromEmail="", fromName="UCZ Trinity Congregation", timeoutMilliseconds=15000 } : new
        {
            row.Id, row.Enabled, row.Host, row.Port, row.EnableSsl, row.Username,
            HasPassword = !string.IsNullOrWhiteSpace(row.Password), row.FromEmail, row.FromName, row.TimeoutMilliseconds
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateSmtpRequest request)
    {
        if (request.Port is < 1 or > 65535) return BadRequest(new { message = "SMTP port must be between 1 and 65535." });
        var row = await _context.SmtpSettings.FirstOrDefaultAsync();
        if (row == null)
        {
            row = new SmtpSetting { Id = Guid.NewGuid(), CreatedDate = DateTime.UtcNow };
            _context.SmtpSettings.Add(row);
        }
        row.Enabled=request.Enabled; row.Host=request.Host.Trim(); row.Port=request.Port; row.EnableSsl=request.EnableSsl;
        row.Username=request.Username.Trim(); row.FromEmail=request.FromEmail.Trim(); row.FromName=request.FromName.Trim();
        row.TimeoutMilliseconds=request.TimeoutMilliseconds; row.UpdatedDate=DateTime.UtcNow; row.Version++;
        if (!string.IsNullOrWhiteSpace(request.Password)) row.Password=request.Password;
        await _context.SaveChangesAsync();
        _provider.InvalidateCache();
        return Ok(new { message="SMTP settings saved." });
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(TestEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest(new { message="Email address is required." });
        await _email.SendTestEmailAsync(request.Email.Trim());
        var latest = await _context.EmailLogs.AsNoTracking().OrderByDescending(x=>x.SentAtUtc).FirstOrDefaultAsync(x=>x.ToEmail==request.Email.Trim() && x.Category=="TestEmail");
        return latest?.Status == "Success" ? Ok(new { message="Test email sent successfully." }) : BadRequest(new { message=latest?.FailureReason ?? "Test email could not be sent." });
    }

    [HttpGet("emails")]
    public async Task<IActionResult> Emails([FromQuery] int limit=100) => Ok(await _context.EmailLogs.AsNoTracking()
        .OrderByDescending(x=>x.SentAtUtc).Take(Math.Clamp(limit,1,500)).ToListAsync());

    public record UpdateSmtpRequest(bool Enabled,string Host,int Port,bool EnableSsl,string Username,string? Password,string FromEmail,string FromName,int TimeoutMilliseconds);
    public record TestEmailRequest(string Email);
}
