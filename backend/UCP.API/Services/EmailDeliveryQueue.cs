using System.Threading.Channels;

namespace UCP.API.Services;

public sealed record QueuedEmail(string ToEmail, string Subject, string HtmlBody, string Action, string[] CcEmails,
    Guid? RetryOfEmailLogId = null, string? CompanionMessage = null);

public interface IEmailDeliveryQueue
{
    bool Enqueue(QueuedEmail email);
    ValueTask<QueuedEmail> DequeueAsync(CancellationToken cancellationToken);
}

public sealed class EmailDeliveryQueue : IEmailDeliveryQueue
{
    private readonly Channel<QueuedEmail> _channel = Channel.CreateBounded<QueuedEmail>(new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false
    });

    public bool Enqueue(QueuedEmail email) => _channel.Writer.TryWrite(email);
    public ValueTask<QueuedEmail> DequeueAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAsync(cancellationToken);
}

public sealed class EmailDeliveryWorker(IEmailDeliveryQueue queue, IServiceScopeFactory scopes, ILogger<EmailDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var email = await queue.DequeueAsync(stoppingToken);
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<EmailService>().DeliverQueuedAsync(email, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected failure in the background email delivery worker.");
            }
        }
    }
}
