using Microsoft.EntityFrameworkCore;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;
using Xunit;

namespace eMilimo.API.Tests.Services;

public sealed class EmailTemplateServiceTests
{
    [Fact]
    public async Task RenderAsync_UsesStoredTemplateAndHtmlEncodesValues()
    {
        await using var db = CreateDb();
        db.SystemConfigs.Add(new SystemConfig { Id = Guid.NewGuid(), ConfigKey = "church_name", ConfigValue = "Trinity & Main" });
        db.EmailTemplates.Add(new EmailTemplate
        {
            Id = Guid.NewGuid(), TemplateKey = "contribution-status", Name = "Contribution status",
            IsEnabled = true, Subject = "{{reference}} — {{status}}",
            HtmlBody = "<p>{{church_name}} | {{recipient_name}} | {{note}}</p>",
            IsSmsEnabled = true, SmsBody = "SMS {{reference}} {{note}}",
            IsWhatsAppEnabled = true, WhatsAppBody = "WA {{recipient_name}} {{status}}"
        });
        await db.SaveChangesAsync();

        var service = new EmailTemplateService(db);
        var rendered = await service.RenderAsync("contribution-status", new Dictionary<string, string?>
        {
            ["reference"] = "CON-42", ["status"] = "Approved",
            ["recipient_name"] = "Jane <Admin>", ["note"] = "A & B"
        });

        Assert.Equal("CON-42 — Approved", rendered.Subject);
        Assert.Contains("Trinity &amp; Main", rendered.HtmlBody);
        Assert.Contains("Jane &lt;Admin&gt;", rendered.HtmlBody);
        Assert.Contains("A &amp; B", rendered.HtmlBody);
        Assert.Equal("SMS CON-42 A & B", rendered.SmsBody);
        Assert.Equal("WA Jane <Admin> Approved", rendered.WhatsAppBody);
    }

    [Fact]
    public void FindUnknownPlaceholders_ReturnsOnlyUnsupportedTokens()
    {
        using var db = CreateDb();
        var service = new EmailTemplateService(db);
        var definition = service.GetDefinition("approval-required");

        var unknown = service.FindUnknownPlaceholders(definition,
            "{{process_name}} {{not_supported}}", "<p>{{recipient_name}} {{also_unknown}}</p>");

        Assert.Equal(["also_unknown", "not_supported"], unknown);
    }

    [Fact]
    public async Task RenderAsync_FallsBackToRegisteredDefaultBeforeCustomization()
    {
        await using var db = CreateDb();
        var service = new EmailTemplateService(db);

        var rendered = await service.RenderAsync("account-activated",
            new Dictionary<string, string?> { ["recipient_name"] = "Mwamba" });

        Assert.True(rendered.IsEnabled);
        Assert.Contains("Mwamba", rendered.HtmlBody);
        Assert.DoesNotContain("{{recipient_name}}", rendered.HtmlBody);
        Assert.Equal("Your Church Administration account is active", rendered.Subject);
        Assert.True(rendered.IsSmsEnabled);
        Assert.Contains("Mwamba", rendered.SmsBody);
        Assert.True(rendered.IsWhatsAppEnabled);
    }

    [Fact]
    public async Task CredentialTemplates_AreEmailOnlyByDefault()
    {
        await using var db = CreateDb();
        var service = new EmailTemplateService(db);

        var reset = await service.RenderAsync("password-reset", new Dictionary<string, string?>
        {
            ["recipient_name"] = "Mwamba", ["reset_link"] = "https://example.test/reset"
        });

        Assert.False(reset.IsSmsEnabled);
        Assert.Empty(reset.SmsBody);
        Assert.False(reset.IsWhatsAppEnabled);
        Assert.Empty(reset.WhatsAppBody);
    }

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
