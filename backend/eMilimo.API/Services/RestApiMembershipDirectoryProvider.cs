using System.Net.Http.Headers;
using System.Text.Json;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Services;

public sealed class RestApiMembershipDirectoryProvider(
    AppDbContext db,
    IHttpClientFactory clients,
    IDataProtectionProvider protection) : IMembershipDirectoryProvider
{
    private readonly IDataProtector _protector = protection.CreateProtector("eMilimo.MembershipDirectory.Secret.v1");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    public string ProviderCode => "REST_API";

    public async Task<IReadOnlyList<MemberDirectoryResult>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        var (client, endpoint) = await ClientAsync(ct);
        using var response = await client.GetAsync(
            $"{endpoint}/members/search?query={Uri.EscapeDataString(query.Trim())}&limit={Math.Clamp(limit, 1, 100)}", ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<DirectorySearchResponse>(stream, JsonOptions, ct);
        return payload?.Items ?? [];
    }

    public async Task<MemberDirectoryResult?> GetAsync(string externalId, CancellationToken ct = default)
    {
        var (client, endpoint) = await ClientAsync(ct);
        using var response = await client.GetAsync($"{endpoint}/members/{Uri.EscapeDataString(externalId)}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<MemberDirectoryResult>(stream, JsonOptions, ct);
    }

    public async Task<MembershipProviderHealth> TestAsync(CancellationToken ct = default)
    {
        try
        {
            var (client, endpoint) = await ClientAsync(ct);
            using var response = await client.GetAsync($"{endpoint}/health", ct);
            return new MembershipProviderHealth(response.IsSuccessStatusCode,
                response.IsSuccessStatusCode ? "The membership API is reachable." : $"The membership API returned HTTP {(int)response.StatusCode}.",
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            return new MembershipProviderHealth(false, $"Membership API connection failed: {ex.Message}", DateTime.UtcNow);
        }
    }

    private async Task<(HttpClient Client, string Endpoint)> ClientAsync(CancellationToken ct)
    {
        var config = await db.MembershipIntegrationConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProviderCode == ProviderCode, ct)
            ?? throw new InvalidOperationException("REST membership provider configuration is missing.");
        if (string.IsNullOrWhiteSpace(config.Endpoint) || !Uri.TryCreate(config.Endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("Configure a valid HTTP or HTTPS membership API endpoint.");
        var client = clients.CreateClient("membership-directory");
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(config.TimeoutSeconds, 1, 300));
        var secret = await db.IntegrationSettings.AsNoTracking().SingleOrDefaultAsync(x => x.IntegrationKey == "membership-rest-api", ct);
        if (secret is not null && !string.IsNullOrWhiteSpace(secret.ProtectedSettings))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _protector.Unprotect(secret.ProtectedSettings));
        return (client, uri.ToString().TrimEnd('/'));
    }

    private sealed record DirectorySearchResponse(IReadOnlyList<MemberDirectoryResult> Items);
}
