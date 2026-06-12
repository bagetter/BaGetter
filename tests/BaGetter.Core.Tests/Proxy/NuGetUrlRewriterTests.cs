using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BaGetter.Core.Tests.Proxy;

public class NuGetUrlRewriterTests
{
    private readonly Mock<IUpstreamHostProvider> _hosts = new();
    private readonly Mock<IUrlGenerator> _url = new();

    private NuGetUrlRewriter CreateTarget(bool enabled)
    {
        return new NuGetUrlRewriter(
            Options.Create(new FullProxyOptions { Enabled = enabled }),
            _hosts.Object,
            _url.Object);
    }

    [Fact]
    public void ReturnsInputWhenDisabled()
    {
        _hosts.Setup(h => h.IsTrustedHost(It.IsAny<string>())).Returns(true);
        _url.Setup(u => u.GetPackageProxyUrl(It.IsAny<string>())).Returns("http://localhost/proxy");
        var target = CreateTarget(enabled: false);

        Assert.Equal(
            "https://api.example.com/icon",
            target.RewriteAssetUrl("https://api.example.com/icon"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReturnsInputWhenNullOrWhitespace(string input)
    {
        var target = CreateTarget(enabled: true);

        Assert.Equal(input, target.RewriteAssetUrl(input));
    }

    [Fact]
    public void ReturnsInputForNonHttpsUrl()
    {
        _hosts.Setup(h => h.IsTrustedHost("api.example.com")).Returns(true);
        var target = CreateTarget(enabled: true);

        Assert.Equal(
            "http://api.example.com/icon",
            target.RewriteAssetUrl("http://api.example.com/icon"));
    }

    [Fact]
    public void ReturnsInputForUntrustedHost()
    {
        _hosts.Setup(h => h.IsTrustedHost(It.IsAny<string>())).Returns(false);
        var target = CreateTarget(enabled: true);

        Assert.Equal(
            "https://evil.example/icon",
            target.RewriteAssetUrl("https://evil.example/icon"));
    }

    [Fact]
    public void ReturnsInputWhenGetPackageProxyUrlReturnsNull()
    {
        _hosts.Setup(h => h.IsTrustedHost("api.example.com")).Returns(true);
        _url.Setup(u => u.GetPackageProxyUrl("https://api.example.com/icon")).Returns((string)null);
        var target = CreateTarget(enabled: true);

        Assert.Equal(
            "https://api.example.com/icon",
            target.RewriteAssetUrl("https://api.example.com/icon"));
    }

    [Fact]
    public void RewritesTrustedHttpsUrlToProxy()
    {
        _hosts.Setup(h => h.IsTrustedHost("api.example.com")).Returns(true);
        _url.Setup(u => u.GetPackageProxyUrl("https://api.example.com/icon"))
            .Returns("http://localhost/v3/proxy/asset?url=encoded");
        var target = CreateTarget(enabled: true);

        Assert.Equal(
            "http://localhost/v3/proxy/asset?url=encoded",
            target.RewriteAssetUrl("https://api.example.com/icon"));
    }
}
