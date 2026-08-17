using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using UCP.Core.Entities;
using UCP.Infrastructure.Data;
using System.Text.Json;

namespace UCP.API.Services;

public sealed record CommunicationSettings(bool SmsEnabled, bool WhatsAppEnabled, string Provider, string AccountSid,
    string AuthToken, string SmsFrom, string WhatsAppFrom, int TimeoutMilliseconds, bool HasAuthToken);

public interface ICommunicationSettingsProvider
{
    Task<CommunicationSettings> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CommunicationSettings settings, CancellationToken cancellationToken = default);
}

public sealed class CommunicationSettingsProvider(IServiceScopeFactory scopes, IDataProtectionProvider protection) : ICommunicationSettingsProvider
{
    private readonly IDataProtector _protector = protection.CreateProtector("UCP.CommunicationSettings.Secret.v1");
    public async Task<CommunicationSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var scope=scopes.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var row=await db.IntegrationSettings.AsNoTracking().FirstOrDefaultAsync(x=>x.IntegrationKey=="communications",cancellationToken);if(row==null)return new(false,false,"Twilio","","","","",15000,false);try{var saved=JsonSerializer.Deserialize<CommunicationSettings>(_protector.Unprotect(row.ProtectedSettings));return saved==null?new(false,false,"Twilio","","","","",15000,false):saved with{HasAuthToken=!string.IsNullOrWhiteSpace(saved.AuthToken)};}catch{return new(false,false,"Twilio","","","","",15000,false);}
    }
    public async Task SaveAsync(CommunicationSettings settings,CancellationToken cancellationToken=default)
    {
        await using var scope=scopes.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var row=await db.IntegrationSettings.FirstOrDefaultAsync(x=>x.IntegrationKey=="communications",cancellationToken);var previous=await GetAsync(cancellationToken);var stored=settings with{AuthToken=string.IsNullOrWhiteSpace(settings.AuthToken)?previous.AuthToken:settings.AuthToken,HasAuthToken=false};var payload=_protector.Protect(JsonSerializer.Serialize(stored));if(row==null)db.IntegrationSettings.Add(new IntegrationSetting{Id=Guid.NewGuid(),IntegrationKey="communications",ProtectedSettings=payload,CreatedDate=DateTime.UtcNow,UpdatedDate=DateTime.UtcNow,Version=1});else{row.ProtectedSettings=payload;row.UpdatedDate=DateTime.UtcNow;row.Version++;}await db.SaveChangesAsync(cancellationToken);
    }
}

public interface ICommunicationService
{
    Task SendSmsAsync(string recipient,string message,CancellationToken cancellationToken=default,string action="General",Guid? retryOfCommunicationLogId=null);
    Task SendWhatsAppAsync(string recipient,string message,CancellationToken cancellationToken=default,string action="General",Guid? retryOfCommunicationLogId=null);
}

public sealed class CommunicationService(ICommunicationSettingsProvider settingsProvider,IHttpClientFactory clients,ISystemLogWriter logs,
    ILogger<CommunicationService> logger,AppDbContext db,IDataProtectionProvider protection):ICommunicationService
{
    private readonly IDataProtector _messageProtector=protection.CreateProtector("UCP.CommunicationLog.RetryPayload.v1");
    public async Task SendSmsAsync(string recipient,string message,CancellationToken cancellationToken=default,string action="General",Guid? retryOfCommunicationLogId=null){var s=await settingsProvider.GetAsync(cancellationToken);if(!s.SmsEnabled){await RecordAsync("SMS",recipient,action,"Failed",null,"SMS notifications are disabled.",message,retryOfCommunicationLogId,cancellationToken);throw new InvalidOperationException("SMS notifications are disabled in System Settings.");}await SendTwilio(s,recipient,message,s.SmsFrom,"SMS",action,retryOfCommunicationLogId,cancellationToken);}
    public async Task SendWhatsAppAsync(string recipient,string message,CancellationToken cancellationToken=default,string action="General",Guid? retryOfCommunicationLogId=null){var s=await settingsProvider.GetAsync(cancellationToken);if(!s.WhatsAppEnabled){await RecordAsync("WhatsApp",recipient,action,"Failed",null,"WhatsApp notifications are disabled.",message,retryOfCommunicationLogId,cancellationToken);throw new InvalidOperationException("WhatsApp notifications are disabled in System Settings.");}await SendTwilio(s,Prefix(recipient),message,Prefix(s.WhatsAppFrom),"WhatsApp",action,retryOfCommunicationLogId,cancellationToken);}
    private async Task SendTwilio(CommunicationSettings s,string recipient,string message,string sender,string channel,string action,Guid? retryOfCommunicationLogId,CancellationToken cancellationToken)
    {
        try{if(!s.Provider.Equals("Twilio",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("The configured communication provider is not supported.");if(string.IsNullOrWhiteSpace(s.AccountSid)||string.IsNullOrWhiteSpace(s.AuthToken)||string.IsNullOrWhiteSpace(sender))throw new InvalidOperationException($"Complete the {channel} provider settings before enabling delivery.");using var request=new HttpRequestMessage(HttpMethod.Post,$"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(s.AccountSid)}/Messages.json");request.Headers.Authorization=new AuthenticationHeaderValue("Basic",Convert.ToBase64String(Encoding.ASCII.GetBytes($"{s.AccountSid}:{s.AuthToken}")));request.Content=new FormUrlEncodedContent(new Dictionary<string,string>{{"To",recipient},{"From",sender},{"Body",message}});var client=clients.CreateClient("communications");client.Timeout=TimeSpan.FromMilliseconds(Math.Clamp(s.TimeoutMilliseconds,5000,120000));using var response=await client.SendAsync(request,cancellationToken);var responseBody=await response.Content.ReadAsStringAsync(cancellationToken);if(!response.IsSuccessStatusCode)throw new InvalidOperationException(ProviderFailure(response,responseBody));var providerId=ProviderMessageId(responseBody);await RecordAsync(channel,recipient,action,"Success",providerId,null,message,retryOfCommunicationLogId,cancellationToken);await logs.WriteAsync(new SystemLog{Level="Information",Category="Communication",Process="Notifications",Action=action,Message=$"{channel} notification accepted by the provider for {Mask(recipient)}."},cancellationToken);}
        catch(Exception ex){logger.LogError(ex,"{Channel} delivery failed.",channel);await RecordAsync(channel,recipient,action,"Failed",null,ex.Message,message,retryOfCommunicationLogId,cancellationToken);await logs.WriteAsync(new SystemLog{Level="Error",Category="Communication",Process="Notifications",Action=action,Message=$"{channel} notification failed for {Mask(recipient)}.",Exception=ex.Message},cancellationToken);throw;}
    }
    private static string Prefix(string value)=>value.StartsWith("whatsapp:",StringComparison.OrdinalIgnoreCase)?value:$"whatsapp:{value}";
    private static string Mask(string value){var clean=value.Replace("whatsapp:","",StringComparison.OrdinalIgnoreCase);return clean.Length<=4?"****":$"***{clean[^4..]}";}
    private static string ProviderFailure(HttpResponseMessage response,string body)
    {
        const int maxMessageLength=400;
        try
        {
            using var json=JsonDocument.Parse(body);
            var root=json.RootElement;
            var code=root.TryGetProperty("code",out var codeValue)?codeValue.ToString():"";
            var reason=root.TryGetProperty("message",out var messageValue)?messageValue.GetString()?.Trim():null;
            if(!string.IsNullOrWhiteSpace(reason))
            {
                reason=new string(reason.Where(c=>!char.IsControl(c)).ToArray());
                if(reason.Length>maxMessageLength)reason=reason[..maxMessageLength];
                return $"Twilio rejected the request{(string.IsNullOrWhiteSpace(code)?"":$" (error {code})")}: {reason}";
            }
        }
        catch(JsonException) { }
        return $"Twilio returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).";
    }
    private static string? ProviderMessageId(string body){try{using var json=JsonDocument.Parse(body);return json.RootElement.TryGetProperty("sid",out var sid)?sid.GetString():null;}catch{return null;}}
    private async Task RecordAsync(string channel,string recipient,string action,string status,string? providerId,string? failure,string message,Guid? retryOf,CancellationToken cancellationToken)
    {
        db.CommunicationLogs.Add(new CommunicationLog{Channel=channel,Provider="Twilio",Recipient=recipient,Action=action,Status=status,ProviderMessageId=providerId,FailureReason=failure is {Length:>1000}?failure[..1000]:failure,ProtectedMessage=_messageProtector.Protect(message),RetryOfCommunicationLogId=retryOf});
        await db.SaveChangesAsync(cancellationToken);
    }
}
