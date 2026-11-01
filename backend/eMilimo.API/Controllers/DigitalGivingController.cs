using eMilimo.API.Authorization;
using eMilimo.Core.Payments;
using eMilimo.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eMilimo.API.Controllers;

[ApiController]
[Route("api/digital-giving")]
[Authorize]
public sealed class DigitalGivingController : ControllerBase
{
    private readonly IDigitalGivingConfigurationService _configuration;
    private readonly IPaymentProviderConfigurationStore _providers;
    private readonly IPaymentProviderRegistry _registry;
    private readonly ICurrentUserService _currentUser;

    public DigitalGivingController(
        IDigitalGivingConfigurationService configuration,
        IPaymentProviderConfigurationStore providers,
        IPaymentProviderRegistry registry,
        ICurrentUserService currentUser)
    {
        _configuration = configuration;
        _providers = providers;
        _registry = registry;
        _currentUser = currentUser;
    }

    [HttpGet("configuration")]
    [RequirePermission("settings.manage")]
    public async Task<IActionResult> GetConfiguration(CancellationToken cancellationToken) =>
        Ok(await _configuration.GetAdministrationViewAsync(cancellationToken));

    [HttpGet("catalog")]
    [RequirePermission("settings.manage")]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken) =>
        Ok(await _configuration.GetMaintenanceCatalogAsync(cancellationToken));

    [HttpPut("configuration")]
    [RequirePermission("settings.manage")]
    public async Task<IActionResult> SaveConfiguration(
        [FromBody] DigitalGivingConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetUserId() ?? throw new UnauthorizedAccessException("User is not authenticated.");
        return Ok(await _configuration.SaveAsync(request, actor, cancellationToken));
    }

    [HttpGet("providers")]
    [RequirePermission("settings.manage")]
    public async Task<IActionResult> GetProviders(CancellationToken cancellationToken) => Ok(new
    {
        availableProviders = _registry.DescribeAll(),
        configurations = await _providers.ListAsync(cancellationToken)
    });

    [HttpPost("providers")]
    [RequirePermission("settings.manage")]
    public async Task<IActionResult> SaveProvider(
        [FromBody] PaymentProviderConfigurationDraft request,
        CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetUserId() ?? throw new UnauthorizedAccessException("User is not authenticated.");
        return Ok(await _providers.SaveAsync(request, actor, cancellationToken));
    }
}
