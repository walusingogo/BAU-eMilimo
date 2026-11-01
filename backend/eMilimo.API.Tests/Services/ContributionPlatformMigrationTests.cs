using Microsoft.Extensions.DependencyInjection;
using BAU.Platform.Notifications;
using Xunit;

namespace eMilimo.API.Tests.Services;

/// <summary>
/// Characterization test validating that the BAU Platform notification contracts
/// are properly registered and functional in the eMilimo dependency injection container.
/// </summary>
public class ContributionPlatformMigrationTests
{
    [Fact]
    public void NotificationRouter_RegisteredInDependencyContainer()
    {
        // Arrange: Set up a minimal DI container matching eMilimo.API startup configuration
        var services = new ServiceCollection();
        
        // Act: Register gateways first, then use the platform extension to register senders
        services.AddSingleton<IEmailGateway, StubEmailGateway>();
        services.AddSingleton<ISmsGateway, StubSmsGateway>();
        services.AddSingleton<IWhatsAppGateway, StubWhatsAppGateway>();
        
        // Use the platform extension to register NotificationRouter and channel senders
        services.AddBauNotificationRouting();
        
        var provider = services.BuildServiceProvider();
        
        // Assert: NotificationRouter can be resolved
        var router = provider.GetRequiredService<NotificationRouter>();
        Assert.NotNull(router);
    }

    [Fact]
    public async Task NotificationRouter_RoutesEmailNotifications()
    {
        // Arrange
        var capturedRequests = new List<NotificationRequest>();
        var emailGateway = new CaptureEmailGateway(capturedRequests);
        
        var services = new ServiceCollection();
        services.AddSingleton<IEmailGateway>(emailGateway);
        services.AddSingleton<ISmsGateway, StubSmsGateway>();
        services.AddSingleton<IWhatsAppGateway, StubWhatsAppGateway>();
        services.AddBauNotificationRouting();
        
        var provider = services.BuildServiceProvider();
        var router = provider.GetRequiredService<NotificationRouter>();
        
        // Act: Send a notification through the router (simulating what ContributionService does)
        var request = new NotificationRequest(
            Channel: NotificationChannel.Email,
            Recipients: [new NotificationRecipient("user@example.com", "Test User")],
            Subject: "Contribution Confirmed",
            Body: "Your contribution has been confirmed",
            Metadata: new Dictionary<string, string>
            {
                { "process", "Contribution" },
                { "recordNumber", "CONTRIB-001" },
                { "status", "CONFIRMED" }
            }
        );
        
        await router.SendAsync(request);
        
        // Assert: Notification was captured by the email gateway
        Assert.Single(capturedRequests);
        var captured = capturedRequests[0];
        Assert.Equal(NotificationChannel.Email, captured.Channel);
        Assert.Contains("user@example.com", captured.Recipients.Select(r => r.Address));
        Assert.Equal("Contribution Confirmed", captured.Subject);
        Assert.NotNull(captured.Metadata);
        Assert.True(captured.Metadata.ContainsKey("recordNumber"));
        Assert.Equal("CONTRIB-001", captured.Metadata["recordNumber"]);
    }
}

// ============================================
// TEST DOUBLES
// ============================================

internal sealed class CaptureEmailGateway : IEmailGateway
{
    private readonly List<NotificationRequest> _capturedRequests;

    public CaptureEmailGateway(List<NotificationRequest> capturedRequests)
        => _capturedRequests = capturedRequests;

    public Task SendEmailAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        _capturedRequests.Add(request);
        return Task.CompletedTask;
    }
}

internal sealed class StubEmailGateway : IEmailGateway
{
    public Task SendEmailAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal sealed class StubSmsGateway : ISmsGateway
{
    public Task SendSmsAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal sealed class StubWhatsAppGateway : IWhatsAppGateway
{
    public Task SendWhatsAppAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

