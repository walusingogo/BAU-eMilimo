using System.Text;
using eMilimo.Core.Payments;
using eMilimo.API.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace eMilimo.API.Controllers;

[ApiController]
[Route("api/public/digital-giving")]
[AllowAnonymous]
[EnableRateLimiting("public-digital-giving")]
public sealed class PublicDigitalGivingController : ControllerBase
{
    private const int MaximumWebhookBytes = 1_048_576;
    private readonly IDigitalGivingConfigurationService _configuration;
    private readonly IMemberVerificationService _memberVerification;
    private readonly IDigitalPaymentIntentService _paymentIntents;
    private readonly IPaymentWebhookProcessor _webhooks;
    private readonly IPublicPaymentStatusRefreshService _statusRefresh;

    public PublicDigitalGivingController(
        IDigitalGivingConfigurationService configuration,
        IMemberVerificationService memberVerification,
        IDigitalPaymentIntentService paymentIntents,
        IPaymentWebhookProcessor webhooks,
        IPublicPaymentStatusRefreshService statusRefresh)
    {
        _configuration = configuration;
        _memberVerification = memberVerification;
        _paymentIntents = paymentIntents;
        _webhooks = webhooks;
        _statusRefresh = statusRefresh;
    }

    [HttpGet("{publicSlug}")]
    public async Task<IActionResult> GetPublicConfiguration(string publicSlug, CancellationToken cancellationToken)
    {
        var configuration = await _configuration.GetPublicViewAsync(publicSlug, cancellationToken);
        return configuration is null ? NotFound(new { message = "Digital Giving page not found." }) : Ok(configuration);
    }

    [HttpGet]
    public async Task<IActionResult> GetDefaultPublicConfiguration(CancellationToken cancellationToken)
    {
        var configuration = await _configuration.GetDefaultPublicViewAsync(cancellationToken);
        return configuration is null ? NotFound(new { message = "Digital Giving is not currently available." }) : Ok(configuration);
    }

    [HttpPost("member-verification/request")]
    [EnableRateLimiting("digital-giving-otp")]
    public async Task<IActionResult> RequestMemberVerification(
        [FromBody] MemberVerificationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _memberVerification.RequestAsync(request.Lookup, ClientFingerprint(), cancellationToken));

    [HttpPost("member-identification")]
    [EnableRateLimiting("digital-giving-otp")]
    public async Task<IActionResult> IdentifyMember(
        [FromBody] MemberIdentificationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _memberVerification.IdentifyAsync(
            request.Lookup,
            request.AdditionalIdentifier,
            ClientFingerprint(),
            cancellationToken));

    [HttpPost("member-verification/verify")]
    [EnableRateLimiting("digital-giving-otp")]
    public async Task<IActionResult> VerifyMember(
        [FromBody] VerifyMemberRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _memberVerification.VerifyAsync(
            request.ChallengeId,
            request.Code,
            ClientFingerprint(),
            cancellationToken));

    [HttpPost("{publicSlug}/payment-intents")]
    public async Task<IActionResult> CreatePaymentIntent(
        string publicSlug,
        [FromBody] CreateDigitalPaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(publicSlug, request.PublicSlug, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "The Digital Giving page does not match the payment request." });
        var result = await _paymentIntents.CreateAsync(request, ClientFingerprint(), cancellationToken);
        return result.IsIdempotentReplay
            ? Ok(result)
            : StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("payment-intents/{paymentReference}/status")]
    public async Task<IActionResult> GetPaymentStatus(
        string paymentReference,
        [FromQuery] string token,
        CancellationToken cancellationToken) =>
        Ok(await _paymentIntents.GetStatusAsync(paymentReference, token, cancellationToken));

    [HttpPost("payment-intents/{paymentReference}/refresh-status")]
    public async Task<IActionResult> RefreshPaymentStatus(
        string paymentReference,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        await _statusRefresh.RefreshAsync(paymentReference, token, HttpContext.TraceIdentifier, cancellationToken);
        return Ok(await _paymentIntents.GetStatusAsync(paymentReference, token, cancellationToken));
    }

    [AcceptVerbs("POST", "PUT", Route = "webhooks/{providerKey}")]
    [EnableRateLimiting("digital-giving-webhook")]
    public async Task<IActionResult> ReceiveWebhook(string providerKey, CancellationToken cancellationToken)
    {
        if (Request.ContentLength > MaximumWebhookBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: false);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        if (Encoding.UTF8.GetByteCount(rawBody) > MaximumWebhookBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        var headers = Request.Headers.ToDictionary(
            x => x.Key,
            x => x.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);
        var result = await _webhooks.ProcessAsync(
            providerKey,
            rawBody,
            headers,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return result.Accepted ? Ok(result) : Unauthorized(result);
    }

    private string ClientFingerprint() =>
        $"{HttpContext.Connection.RemoteIpAddress}|{Request.Headers.UserAgent.ToString()}";

    public sealed record MemberVerificationRequest(string Lookup);
    public sealed record VerifyMemberRequest(Guid ChallengeId, string Code);
    public sealed record MemberIdentificationRequest(string Lookup, string? AdditionalIdentifier);
}
