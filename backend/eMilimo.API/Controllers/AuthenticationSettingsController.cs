using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using eMilimo.API.Services;

namespace eMilimo.API.Controllers;

[ApiController,Route("api/authentication-settings"),Authorize(Roles="Admin,SuperAdmin")]
public sealed class AuthenticationSettingsController(IDirectoryAuthenticationSettingsProvider provider,IDirectoryAuthenticationService directory):ControllerBase
{
    [HttpGet]public async Task<IActionResult> Get(){var x=await provider.GetAsync();return Ok(new{x.Mode,x.Server,x.Port,x.UseSsl,x.Domain,x.BaseDn,x.BindUsername,BindPassword="",x.UserAttribute,x.AllowLocalFallback,x.HasBindPassword});}
    [HttpPut]public async Task<IActionResult> Save(AuthenticationSettingsRequest r){if(r.Mode is not ("Local" or "ActiveDirectory" or "Hybrid"))return BadRequest(new{message="Select Local, ActiveDirectory or Hybrid authentication."});if(r.Mode!="Local"&&string.IsNullOrWhiteSpace(r.Server))return BadRequest(new{message="Active Directory server is required."});if(r.Port is <1 or >65535)return BadRequest(new{message="Enter a valid directory port."});await provider.SaveAsync(new(r.Mode,r.Server.Trim(),r.Port,r.UseSsl,r.Domain.Trim(),r.BaseDn.Trim(),r.BindUsername.Trim(),r.BindPassword??"",r.UserAttribute,r.AllowLocalFallback,false));return Ok(new{message="Authentication settings saved. Restart is not required."});}
    [HttpPost("test")]public async Task<IActionResult> Test(TestDirectoryRequest r){var result=await directory.AuthenticateAsync(r.Identifier,r.Password);return result.Success?Ok(new{message="Active Directory authentication succeeded."}):BadRequest(new{message=result.ServiceAvailable?"Active Directory rejected the supplied credentials.":"Active Directory is unavailable. Check the server, port, TLS and bind settings."});}
    public sealed record AuthenticationSettingsRequest(string Mode,string Server,int Port,bool UseSsl,string Domain,string BaseDn,string BindUsername,string? BindPassword,string UserAttribute,bool AllowLocalFallback);
    public sealed record TestDirectoryRequest(string Identifier,string Password);
}
