using eMilimo.API.Services;
using Xunit;

namespace eMilimo.API.Tests.Services;

public sealed class CommunicationSettingsTests
{
    [Theory]
    [InlineData(CommunicationProviders.Brevo)]
    [InlineData(CommunicationProviders.Twilio)]
    [InlineData(CommunicationProviders.Independent)]
    public void Catalog_ExposesSupportedProviderForBothChannels(string provider)
    {
        var option = Assert.Single(CommunicationProviders.Catalog, item => item.Key == provider);

        Assert.Contains("sms", option.Channels);
        Assert.Contains("whatsapp", option.Channels);
    }

    [Fact]
    public void ValidateSettings_AcceptsCompleteBrevoConfiguration()
    {
        var settings = new CommunicationChannelSettings
        {
            Enabled = true,
            Provider = CommunicationProviders.Brevo,
            Sender = "eMilimo",
            Secret = "api-key"
        };

        Assert.Null(CommunicationService.ValidateSettings(settings, "SMS", false));
    }

    [Fact]
    public void ValidateSettings_RejectsUnsafeIndependentEndpoint()
    {
        var settings = new CommunicationChannelSettings
        {
            Enabled = true,
            Provider = CommunicationProviders.Independent,
            Sender = "eMilimo",
            Secret = "Bearer secret",
            Endpoint = "http://messaging.example.com/send"
        };

        Assert.Contains("HTTPS", CommunicationService.ValidateSettings(settings, "SMS", false));
    }

    [Fact]
    public void ValidateSettings_AcceptsLoopbackIndependentEndpointForDevelopment()
    {
        var settings = new CommunicationChannelSettings
        {
            Enabled = true,
            Provider = CommunicationProviders.Independent,
            Sender = "+260000000000",
            Secret = "Bearer secret",
            Endpoint = "http://localhost:5050/messages",
            AuthHeaderName = "Authorization"
        };

        Assert.Null(CommunicationService.ValidateSettings(settings, "WhatsApp", false));
    }
}
