using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UCP.API.Services;
using UCP.Core.Services;

namespace UCP.API.Authorization;

[AttributeUsage(AttributeTargets.Method|AttributeTargets.Class,AllowMultiple=true)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permission):base(typeof(RequirePermissionFilter)){Arguments=[permission];}
}
public sealed class RequirePermissionFilter : IAsyncAuthorizationFilter
{
    private readonly string _permission;private readonly IRolePermissionService _permissions;private readonly ICurrentUserService _current;
    public RequirePermissionFilter(string permission,IRolePermissionService permissions,ICurrentUserService current){_permission=permission;_permissions=permissions;_current=current;}
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context){if(!await _permissions.HasAsync(_current.GetUserRole(),_permission))context.Result=new ObjectResult(new{Message=$"Permission '{_permission}' is required."}){StatusCode=403};}
}
