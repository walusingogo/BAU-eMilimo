using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using UCP.Infrastructure.Data;
using UCP.API.Services;

namespace UCP.API.Controllers;

[ApiController,Route("api/communication-settings"),Authorize(Roles="Admin,SuperAdmin")]
public sealed class CommunicationSettingsController(ICommunicationSettingsProvider provider,ICommunicationService communication,
    AppDbContext db,IDataProtectionProvider protection):ControllerBase
{
    private readonly IDataProtector _messageProtector=protection.CreateProtector("UCP.CommunicationLog.RetryPayload.v1");
    [HttpGet]public async Task<IActionResult> Get(){var x=await provider.GetAsync();return Ok(new{x.SmsEnabled,x.WhatsAppEnabled,x.Provider,x.AccountSid,AuthToken="",x.SmsFrom,x.WhatsAppFrom,x.TimeoutMilliseconds,x.HasAuthToken});}
    [HttpPut]public async Task<IActionResult> Save(CommunicationSettingsRequest r){if(r.TimeoutMilliseconds is <5000 or >120000)return BadRequest(new{message="Timeout must be between 5 and 120 seconds."});if(!r.Provider.Equals("Twilio",StringComparison.OrdinalIgnoreCase))return BadRequest(new{message="Select a supported provider."});await provider.SaveAsync(new(r.SmsEnabled,r.WhatsAppEnabled,r.Provider,r.AccountSid.Trim(),r.AuthToken??"",r.SmsFrom.Trim(),r.WhatsAppFrom.Trim(),r.TimeoutMilliseconds,false));return Ok(new{message="SMS and WhatsApp settings saved."});}
    [HttpPost("test")]public async Task<IActionResult> Test(TestCommunicationRequest r){if(string.IsNullOrWhiteSpace(r.Recipient))return BadRequest(new{message="Enter a test recipient with country code."});try{if(r.Channel.Equals("sms",StringComparison.OrdinalIgnoreCase))await communication.SendSmsAsync(r.Recipient,"UCP SMS integration test successful.",action:"TestSMS");else if(r.Channel.Equals("whatsapp",StringComparison.OrdinalIgnoreCase))await communication.SendWhatsAppAsync(r.Recipient,"UCP WhatsApp integration test successful.",action:"TestWhatsApp");else return BadRequest(new{message="Select SMS or WhatsApp."});return Ok(new{message=$"{r.Channel} test sent."});}catch(Exception ex){return BadRequest(new{message=ex.Message});}}
    [HttpGet("history")]public async Task<IActionResult> History([FromQuery]int limit=100)=>Ok(await db.CommunicationLogs.AsNoTracking().OrderByDescending(x=>x.SentAtUtc).Take(Math.Clamp(limit,1,500)).Select(x=>new{x.Id,x.SentAtUtc,x.Channel,x.Provider,x.Recipient,x.Action,x.Status,x.ProviderMessageId,x.FailureReason,x.RetryOfCommunicationLogId,CanRetry=x.Status=="Failed"&&x.ProtectedMessage!=null}).ToListAsync());
    [HttpPost("history/{id:guid}/retry")]public async Task<IActionResult> Retry(Guid id)
    {
        var original=await db.CommunicationLogs.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);
        if(original==null)return NotFound(new{message="Communication delivery record was not found."});
        if(original.Status!="Failed")return BadRequest(new{message="Only failed SMS or WhatsApp deliveries can be retried."});
        if(string.IsNullOrWhiteSpace(original.ProtectedMessage))return BadRequest(new{message="This record does not contain a retryable message."});
        string message;try{message=_messageProtector.Unprotect(original.ProtectedMessage);}catch{return BadRequest(new{message="The retained message can no longer be decrypted and cannot be retried."});}
        try
        {
            if(original.Channel.Equals("SMS",StringComparison.OrdinalIgnoreCase))await communication.SendSmsAsync(original.Recipient,message,action:original.Action,retryOfCommunicationLogId:original.Id);
            else if(original.Channel.Equals("WhatsApp",StringComparison.OrdinalIgnoreCase))await communication.SendWhatsAppAsync(original.Recipient,message,action:original.Action,retryOfCommunicationLogId:original.Id);
            else return BadRequest(new{message="The communication channel is not supported."});
            return Ok(new{message=$"{original.Channel} retry sent successfully."});
        }
        catch(Exception ex){return BadRequest(new{message=ex.Message});}
    }
    public sealed record CommunicationSettingsRequest(bool SmsEnabled,bool WhatsAppEnabled,string Provider,string AccountSid,string? AuthToken,string SmsFrom,string WhatsAppFrom,int TimeoutMilliseconds);
    public sealed record TestCommunicationRequest(string Channel,string Recipient);
}
