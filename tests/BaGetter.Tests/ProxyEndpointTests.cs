using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BaGetter.Tests;

public class ProxyEndpointTests : IDisposable
{
    private readonly BaGetterApplication _app;
    private readonly HttpClient _client;

    public ProxyEndpointTests(ITestOutputHelper output)
    {
        // Full proxy enabled without an upstream: the trusted host set is empty, so
        // the asset proxy rejects every URL. This exercises the SSRF guards and the
        // endpoint wiring without any outbound network call.
        _app = new BaGetterApplication(
            output,
            inMemoryConfiguration: config => config["FullProxy:Enabled"] = "true");
        _client = _app.CreateClient();
    }

    [Fact]
    public async Task RejectsNonHttpsUrl()
    {
        using var response = await _client.GetAsync("v3/proxy/asset?url=http://localhost/x");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectsUntrustedHost()
    {
        using var response = await _client.GetAsync("v3/proxy/asset?url=https://untrusted.example/x");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public void Dispose()
    {
        _app.Dispose();
    }
}
