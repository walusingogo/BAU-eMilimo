using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;
using System.Text.Json;

namespace eMilimo.API.Services;

public sealed record DirectoryAuthenticationSettings(string Mode,string Server,int Port,bool UseSsl,string Domain,string BaseDn,string BindUsername,string BindPassword,string UserAttribute,bool AllowLocalFallback,bool HasBindPassword);
public sealed record DirectoryAuthenticationResult(bool Success,bool ServiceAvailable);
public interface IDirectoryAuthenticationSettingsProvider{Task<DirectoryAuthenticationSettings> GetAsync(CancellationToken cancellationToken=default);Task SaveAsync(DirectoryAuthenticationSettings settings,CancellationToken cancellationToken=default);}

public sealed class DirectoryAuthenticationSettingsProvider(IServiceScopeFactory scopes,IDataProtectionProvider protection):IDirectoryAuthenticationSettingsProvider
{
    private readonly IDataProtector _protector=protection.CreateProtector("eMilimo.DirectoryAuthentication.Secret.v1");
    public async Task<DirectoryAuthenticationSettings> GetAsync(CancellationToken cancellationToken=default){await using var scope=scopes.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var row=await db.IntegrationSettings.AsNoTracking().FirstOrDefaultAsync(x=>x.IntegrationKey=="directory-auth",cancellationToken);if(row==null)return new("Local","",389,false,"","","","","sAMAccountName",true,false);try{var saved=JsonSerializer.Deserialize<DirectoryAuthenticationSettings>(_protector.Unprotect(row.ProtectedSettings));return saved==null?new("Local","",389,false,"","","","","sAMAccountName",true,false):saved with{HasBindPassword=!string.IsNullOrWhiteSpace(saved.BindPassword)};}catch{return new("Local","",389,false,"","","","","sAMAccountName",true,false);}}
    public async Task SaveAsync(DirectoryAuthenticationSettings s,CancellationToken cancellationToken=default){await using var scope=scopes.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var row=await db.IntegrationSettings.FirstOrDefaultAsync(x=>x.IntegrationKey=="directory-auth",cancellationToken);var previous=await GetAsync(cancellationToken);var stored=s with{BindPassword=string.IsNullOrWhiteSpace(s.BindPassword)?previous.BindPassword:s.BindPassword,HasBindPassword=false};var payload=_protector.Protect(JsonSerializer.Serialize(stored));if(row==null)db.IntegrationSettings.Add(new IntegrationSetting{Id=Guid.NewGuid(),IntegrationKey="directory-auth",ProtectedSettings=payload,CreatedDate=DateTime.UtcNow,UpdatedDate=DateTime.UtcNow,Version=1});else{row.ProtectedSettings=payload;row.UpdatedDate=DateTime.UtcNow;row.Version++;}await db.SaveChangesAsync(cancellationToken);}
}

public interface IDirectoryAuthenticationService{Task<DirectoryAuthenticationResult> AuthenticateAsync(string identifier,string password,CancellationToken cancellationToken=default);}
public sealed class DirectoryAuthenticationService(IDirectoryAuthenticationSettingsProvider provider,ILogger<DirectoryAuthenticationService> logger):IDirectoryAuthenticationService
{
    public async Task<DirectoryAuthenticationResult> AuthenticateAsync(string identifier,string password,CancellationToken cancellationToken=default)
    {
        var s=await provider.GetAsync(cancellationToken);if(s.Mode.Equals("Local",StringComparison.OrdinalIgnoreCase))return new(false,true);if(string.IsNullOrWhiteSpace(s.Server))return new(false,false);
        try{return await Task.Run(()=>Authenticate(s,identifier,password),cancellationToken);}catch(LdapException ex){logger.LogWarning("Active Directory authentication was unavailable or rejected a bind. LDAP code {Code}.",ex.ErrorCode);return new(false,ex.ErrorCode==49);}catch(Exception ex){logger.LogError(ex,"Active Directory authentication failed unexpectedly.");return new(false,false);}
    }
    private static DirectoryAuthenticationResult Authenticate(DirectoryAuthenticationSettings s,string identifier,string password)
    {
        var ldapId=new LdapDirectoryIdentifier(s.Server,s.Port,false,false);using var connection=new LdapConnection(ldapId){AuthType=AuthType.Basic,Timeout=TimeSpan.FromSeconds(15)};connection.SessionOptions.ProtocolVersion=3;connection.SessionOptions.SecureSocketLayer=s.UseSsl;
        string userDn;if(!string.IsNullOrWhiteSpace(s.BaseDn)&&!string.IsNullOrWhiteSpace(s.BindUsername)){connection.Credential=new NetworkCredential(s.BindUsername,s.BindPassword);connection.Bind();var attribute=AllowedAttribute(s.UserAttribute);var request=new SearchRequest(s.BaseDn,$"({attribute}={Escape(identifier)})",SearchScope.Subtree,"distinguishedName");var response=(SearchResponse)connection.SendRequest(request);if(response.Entries.Count!=1)return new(false,true);userDn=response.Entries[0].DistinguishedName;}else userDn=string.IsNullOrWhiteSpace(s.Domain)?identifier:$"{s.Domain}\\{identifier}";
        using var userConnection=new LdapConnection(ldapId){AuthType=AuthType.Basic,Credential=new NetworkCredential(userDn,password),Timeout=TimeSpan.FromSeconds(15)};userConnection.SessionOptions.ProtocolVersion=3;userConnection.SessionOptions.SecureSocketLayer=s.UseSsl;userConnection.Bind();return new(true,true);
    }
    private static string AllowedAttribute(string value)=>value is "sAMAccountName" or "userPrincipalName" or "mail"?value:"sAMAccountName";
    private static string Escape(string value)=>value.Replace("\\","\\5c").Replace("*","\\2a").Replace("(","\\28").Replace(")","\\29").Replace("\0","\\00");
}
