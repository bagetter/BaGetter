using System;
using System.Threading;
using BaGetter.Protocol;
using BaGetter.Protocol.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BaGetter.Core.Tests.Proxy;

public class UpstreamHostProviderTests
{
    [Fact]
    public async System.Threading.Tasks.Task ResolvesTrustedHostsFromPackageSourceAndServiceIndex()
    {
        var serviceIndexClient = new Mock<IServiceIndexClient>();
        serviceIndexClient
            .Setup(c => c.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceIndexResponse
            {
                Resources =
                [
                    new ServiceIndexItem { ResourceUrl = "https://search.feed.example/query", Type = "SearchQueryService" },
                    new ServiceIndexItem { ResourceUrl = "https://cdn.feed.example/v3-flatcontainer/", Type = "PackageBaseAddress/3.0.0" },
                ]
            });
        var factory = new Mock<NuGetClientFactory>();
        factory.Setup(f => f.CreateServiceIndexClient()).Returns(serviceIndexClient.Object);

        var target = CreateTarget(factory, mirrorEnabled: true, fullProxyEnabled: true);

        await target.EnsureResolvedAsync(default);

        Assert.True(target.IsTrustedHost("index.feed.example"));
        Assert.True(target.IsTrustedHost("search.feed.example"));
        Assert.True(target.IsTrustedHost("cdn.feed.example"));
        Assert.False(target.IsTrustedHost("evil.example"));
    }

    [Fact]
    public async System.Threading.Tasks.Task DoesNotResolveWhenFullProxyDisabled()
    {
        var factory = new Mock<NuGetClientFactory>();
        var target = CreateTarget(factory, mirrorEnabled: true, fullProxyEnabled: false);

        await target.EnsureResolvedAsync(default);

        Assert.False(target.IsTrustedHost("index.feed.example"));
        factory.Verify(f => f.CreateServiceIndexClient(), Times.Never);
    }

    private static UpstreamHostProvider CreateTarget(Mock<NuGetClientFactory> factory, bool mirrorEnabled, bool fullProxyEnabled)
    {
        return new UpstreamHostProvider(
            factory.Object,
            Options.Create(new MirrorOptions
            {
                Enabled = mirrorEnabled,
                PackageSource = new Uri("https://index.feed.example/v3/index.json"),
            }),
            Options.Create(new FullProxyOptions { Enabled = fullProxyEnabled }),
            new Mock<ILogger<UpstreamHostProvider>>().Object);
    }
}
