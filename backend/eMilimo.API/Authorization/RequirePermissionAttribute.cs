using BAU.Platform.Authorization.AspNetCore;

namespace eMilimo.API.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string permission) : RequireCapabilityAttribute(permission);
