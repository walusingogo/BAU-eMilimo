using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCP.API.Platform;

namespace UCP.API.Controllers;

[ApiController]
[Route("api/platform")]
[Authorize]
public sealed class PlatformController(IPlatformModuleCatalog modules, IPlatformNode node) : ControllerBase
{
    [HttpGet("modules")]
    public IActionResult Modules() => Ok(modules.GetModules().Where(x => x.Enabled));

    [HttpGet("node")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Node(CancellationToken cancellationToken) => Ok(new
    {
        node.Current.NodeId,
        node.Current.HostName,
        node.Current.Environment,
        node.Current.Product,
        node.Current.Instance,
        DeploymentId = await node.GetDeploymentIdentityAsync(cancellationToken)
    });
}
