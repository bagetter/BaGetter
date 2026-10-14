using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Upstream;

public class FallbackUpstreamClientTests
{
    [Fact]
    public async Task DownloadPackageOrNullAsync_UsesNextMirrorWhenFirstReturnsNull()
    {
        var first = new Mock<IUpstreamClient>();
        var second = new Mock<IUpstreamClient>();
        var expected = new MemoryStream(new byte[] { 1, 2, 3 });

        first.Setup(x => x.DownloadPackageOrNullAsync("a", It.IsAny<NuGetVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream)null);
        second.Setup(x => x.DownloadPackageOrNullAsync("a", It.IsAny<NuGetVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var target = new FallbackUpstreamClient(
            [first.Object, second.Object],
            Mock.Of<ILogger<FallbackUpstreamClient>>());

        var result = await target.DownloadPackageOrNullAsync("a", new NuGetVersion("1.0.0"), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ListPackageVersionsAsync_UnionsVersionsAcrossMirrors()
    {
        var first = new Mock<IUpstreamClient>();
        var second = new Mock<IUpstreamClient>();

        first.Setup(x => x.ListPackageVersionsAsync("a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NuGetVersion> { new("1.0.0"), new("2.0.0") });
        second.Setup(x => x.ListPackageVersionsAsync("a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NuGetVersion> { new("2.0.0"), new("3.0.0") });

        var target = new FallbackUpstreamClient(
            [first.Object, second.Object],
            Mock.Of<ILogger<FallbackUpstreamClient>>());

        var result = await target.ListPackageVersionsAsync("a", CancellationToken.None);

        // Union of both mirrors, deduped — neither feed shadows the other.
        Assert.Equal(
            new[] { new NuGetVersion("1.0.0"), new NuGetVersion("2.0.0"), new NuGetVersion("3.0.0") },
            result.OrderBy(v => v).ToArray());
    }

    [Fact]
    public async Task ListPackagesAsync_UnionsMetadataAcrossMirrors_EarlierMirrorWinsPerVersion()
    {
        var first = new Mock<IUpstreamClient>();
        var second = new Mock<IUpstreamClient>();

        first.Setup(x => x.ListPackagesAsync("a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Package>
            {
                new() { Id = "a", Version = new NuGetVersion("1.0.0"), Title = "from-first" },
            });
        second.Setup(x => x.ListPackagesAsync("a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Package>
            {
                new() { Id = "a", Version = new NuGetVersion("1.0.0"), Title = "from-second" },
                new() { Id = "a", Version = new NuGetVersion("2.0.0"), Title = "from-second" },
            });

        var target = new FallbackUpstreamClient(
            [first.Object, second.Object],
            Mock.Of<ILogger<FallbackUpstreamClient>>());

        var result = await target.ListPackagesAsync("a", CancellationToken.None);

        Assert.Equal(2, result.Count);
        // Earlier (higher-priority) mirror wins for a version present on both.
        Assert.Equal("from-first", result.Single(p => p.Version == new NuGetVersion("1.0.0")).Title);
        Assert.Contains(result, p => p.Version == new NuGetVersion("2.0.0"));
    }
}
