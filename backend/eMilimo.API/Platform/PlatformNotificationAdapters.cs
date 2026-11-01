using BAU.Platform.Notifications;
using eMilimo.API.Services;

namespace eMilimo.API.Platform;

public sealed class EmailGatewayAdapter(EmailService emailService) : IEmailGateway
{
    public async Task SendEmailAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        foreach (var recipient in request.Recipients)
        {
            await emailService.SendAsync(
                recipient.Address,
                request.Subject ?? "eMilimo notification",
                request.Body,
                cancellationToken);
        }
    }
}

public sealed class NoopSmsGateway : ISmsGateway
{
    public Task SendSmsAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class NoopWhatsAppGateway : IWhatsAppGateway
{
    public Task SendWhatsAppAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
