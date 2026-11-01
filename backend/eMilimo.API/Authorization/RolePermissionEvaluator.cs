using BAU.Platform.Authorization.AspNetCore;
using BAU.Platform.Identity;
using eMilimo.API.Services;

namespace eMilimo.API.Authorization;

/// <summary>Adapts eMilimo's database-backed role permissions to BAU capability policies.</summary>
public sealed class RolePermissionEvaluator(IRolePermissionService permissions, IWorkflowAuthorizationService workflow) : IPermissionEvaluator
{
    public async ValueTask<bool> HasPermissionAsync(
        ICurrentActor actor,
        string permission,
        CancellationToken cancellationToken = default)
    {
        foreach (var role in actor.Roles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await permissions.HasAsync(role, permission)) return true;
        }

        if (actor.UserId.HasValue && (await workflow.GetPermissionKeysAsync(actor.UserId.Value, cancellationToken)).Contains(permission, StringComparer.OrdinalIgnoreCase)) return true;

        return false;
    }
}
