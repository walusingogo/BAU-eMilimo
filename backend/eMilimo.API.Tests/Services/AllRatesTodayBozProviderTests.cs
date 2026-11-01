using System.Net;
using System.Text;
using eMilimo.API.Services;
using Xunit;

namespace eMilimo.API.Tests.Services;

public sealed class AllRatesTodayBozProviderTests
{
    [Fact]
    public async Task GetLatestAsync_KeepsDirectBozRates_AndAddsAuditableReferenceCrossRates()
    {
        const string boz = """
            {"bank":"boz","rate_date":"2026-10-13","stale":false,
             "rates":[
               {"base":"USD","quote":"ZMW","type":"middle","value":19.9},
               {"base":"CNY","quote":"ZMW","type":"middle","value":3.0}
             ],
             "attribution":{"source":"BoZ direct","url":"https://example.test/boz"}}
            """;
        const string reference = """
            {"rate_date":"2026-10-14","base":"USD",
             "rates":[
               {"base":"USD","quote":"ZMW","value":20.0},
               {"base":"USD","quote":"CNY","value":7.0},
               {"base":"USD","quote":"EUR","value":0.8},
               {"base":"USD","quote":"ZAR","value":16.0}
             ],
             "attribution":{"source":"Daily reference","url":"https://example.test/reference"}}
            """;
        var client = new HttpClient(new StubHandler(request =>
            Json(request.RequestUri!.AbsolutePath.Contains("reference-rate") ? reference : boz)));
        var provider = new AllRatesTodayBozProvider(new StubFactory(client), new StubSettings());

        var table = await provider.GetLatestAsync();

        var cny = Assert.Single(table.Rates.Where(x => x.FromCurrencyCode == "CNY"));
        Assert.False(cny.Derived);
        Assert.Equal(3.0m, cny.Rate);
        Assert.Equal(new DateOnly(2026, 10, 13), cny.RateDate);

        var eur = Assert.Single(table.Rates.Where(x => x.FromCurrencyCode == "EUR"));
        Assert.True(eur.Derived);
        Assert.Equal(25m, eur.Rate);
        Assert.Equal(new DateOnly(2026, 10, 14), eur.RateDate);
        Assert.Contains("reference-rate", eur.SourceReference);

        var zar = Assert.Single(table.Rates.Where(x => x.FromCurrencyCode == "ZAR"));
        Assert.True(zar.Derived);
        Assert.Equal(1.25m, zar.Rate);
    }

    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(value, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response(request));
    }

    private sealed class StubFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubSettings : IFcyProviderSettings
    {
        public Task<(bool HasApiKey, bool UseOpenEndpoint)> ViewAsync(CancellationToken ct = default) => Task.FromResult((false, true));
        public Task SaveAsync(string? apiKey, bool useOpenEndpoint, CancellationToken ct = default) => Task.CompletedTask;
        public Task<FcyProviderSecret> GetAsync(CancellationToken ct = default) => Task.FromResult(new FcyProviderSecret("", true));
    }
}
