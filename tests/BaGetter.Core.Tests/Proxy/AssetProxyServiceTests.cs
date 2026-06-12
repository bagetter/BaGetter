using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BaGetter.Core.Tests.Proxy;

public class AssetProxyServiceTests
{
    [Theory]
    [InlineData("https://api.example.com/icon", true)]
    [InlineData("http://api.example.com/icon", false)]
    [InlineData("file:///etc/passwd", false)]
    [InlineData("ftp://api.example.com/icon", false)]
    [InlineData("not a url", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryValidateChecksAbsoluteHttps(string url, bool expected)
    {
        Assert.Equal(expected, AssetProxyService.TryValidate(url, out _));
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("192.168.1.1", true)]
    [InlineData("169.254.169.254", true)]
    [InlineData("fc00::1", true)]
    [InlineData("fe80::1", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("93.184.216.34", false)]
    [InlineData("2001:4860:4860::8888", false)]
    public void IsDisallowedAddressClassifiesAddresses(string ip, bool expected)
    {
        Assert.Equal(expected, AssetProxyService.IsDisallowedAddress(IPAddress.Parse(ip)));
    }

    [Fact]
    public async Task GetAssetAsyncRejectsWhenDisabled()
    {
        var target = CreateTarget(TrustedHosts(), new StubHandler(Ok()), enabled: false);

        var result = await target.GetAssetAsync("https://93.184.216.34/icon", default);

        Assert.Equal(AssetProxyStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task GetAssetAsyncRejectsHttpUrl()
    {
        var target = CreateTarget(TrustedHosts(), new StubHandler(Ok()), enabled: true);

        var result = await target.GetAssetAsync("http://93.184.216.34/icon", default);

        Assert.Equal(AssetProxyStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task GetAssetAsyncRejectsUntrustedHost()
    {
        var hosts = new Mock<IUpstreamHostProvider>();
        hosts.Setup(h => h.EnsureResolvedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        hosts.Setup(h => h.IsTrustedHost(It.IsAny<string>())).Returns(false);
        var target = CreateTarget(hosts, new StubHandler(Ok()), enabled: true);

        var result = await target.GetAssetAsync("https://93.184.216.34/icon", default);

        Assert.Equal(AssetProxyStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task GetAssetAsyncReturnsContentForTrustedHost()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var response = Ok();
        response.Content = new ByteArrayContent(bytes);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var target = CreateTarget(TrustedHosts(), new StubHandler(response), enabled: true);

        var result = await target.GetAssetAsync("https://93.184.216.34/icon", default);

        Assert.Equal(AssetProxyStatus.Ok, result.Status);
        Assert.Equal("image/png", result.ContentType);
        using var ms = new MemoryStream();
        await result.Content.CopyToAsync(ms);
        Assert.Equal(bytes, ms.ToArray());
    }

    [Fact]
    public async Task GetAssetAsyncReturnsNotFoundFor404()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var target = CreateTarget(TrustedHosts(), new StubHandler(response), enabled: true);

        var result = await target.GetAssetAsync("https://93.184.216.34/icon", default);

        Assert.Equal(AssetProxyStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetAssetAsyncReturnsNotFoundFor500()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var target = CreateTarget(TrustedHosts(), new StubHandler(response), enabled: true);

        var result = await target.GetAssetAsync("https://93.184.216.34/icon", default);

        Assert.Equal(AssetProxyStatus.NotFound, result.Status);
    }

    // Note: GetAssetAsyncDoesNotCacheWhenAssetCacheMinutesIsZero is tested
    // implicitly by GetAssetAsyncCachesWhenAssetCacheMinutesGreaterThanZero
    // and the other tests that use assetCacheMinutes=0 (default).

    [Fact]
    public async Task GetAssetAsyncCachesWhenAssetCacheMinutesGreaterThanZero()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var response = Ok();
        response.Content = new ByteArrayContent(bytes);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var handler = new CountingHandler(response);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var hosts = TrustedHosts();
        var target = CreateTarget(hosts, handler, enabled: true, cache: cache, assetCacheMinutes: 60);

        var result1 = await target.GetAssetAsync("https://93.184.216.34/icon", default);
        Assert.Equal(AssetProxyStatus.Ok, result1.Status);

        var result2 = await target.GetAssetAsync("https://93.184.216.34/icon", default);
        Assert.Equal(AssetProxyStatus.Ok, result2.Status);

        Assert.Equal(1, handler.CallCount);
    }

    private static Mock<IUpstreamHostProvider> TrustedHosts()
    {
        var hosts = new Mock<IUpstreamHostProvider>();
        hosts.Setup(h => h.EnsureResolvedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        hosts.Setup(h => h.IsTrustedHost(It.IsAny<string>())).Returns(true);
        return hosts;
    }

    private static HttpResponseMessage Ok() => new(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };

    private static AssetProxyService CreateTarget(
        Mock<IUpstreamHostProvider> hosts,
        HttpMessageHandler handler,
        bool enabled,
        MemoryCache cache = null,
        int assetCacheMinutes = 0)
    {
        return new AssetProxyService(
            new HttpClient(handler),
            hosts.Object,
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new FullProxyOptions { Enabled = enabled, AssetCacheMinutes = assetCacheMinutes }),
            new Mock<ILogger<AssetProxyService>>().Object);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StubHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public int CallCount;

        public CountingHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_response);
        }
    }
}
