using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using BaGetter.Core.Indexing;
using BaGetter.Core.Upstream;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Feeds;

/// <summary>
/// Verifies that mirroring is enabled per-feed: upstream is hit when the requesting feed
/// has mirror enabled, and skipped when the requesting feed has mirror disabled.
/// </summary>
public class PerFeedMirrorTests
{
    private static readonly string _packageId = "SomePackage";
    private static readonly NuGetVersion _packageVersion = new NuGetVersion("1.0.0");
    private static readonly CancellationToken _cancellation = CancellationToken.None;

    private readonly Mock<IPackageDatabase> _db;
    private readonly Mock<IUpstreamClient> _upstreamClient;
    private readonly Mock<IUpstreamClientFactory> _upstreamFactory;
    private readonly Mock<IPackageIndexingService> _indexer;

    private readonly Feed _feedWithMirror;
    private readonly Feed _feedWithoutMirror;

    public PerFeedMirrorTests()
    {
        _db = new Mock<IPackageDatabase>();
        _upstreamClient = new Mock<IUpstreamClient>();
        _upstreamFactory = new Mock<IUpstreamClientFactory>();
        _indexer = new Mock<IPackageIndexingService>();

        _feedWithMirror = new Feed
        {
            Id = Guid.NewGuid(),
            Slug = "mirror-feed",
            Name = "Mirror Feed",
            MirrorEnabled = true,
        };

        _feedWithoutMirror = new Feed
        {
            Id = Guid.NewGuid(),
            Slug = "local-feed",
            Name = "Local Feed",
            MirrorEnabled = false,
        };

        // Factory returns the mock upstream client only for the mirror-enabled feed.
        // For the disabled feed, return the disabled client (no-op).
        var disabledClient = new Mock<IUpstreamClient>();
        disabledClient
            .Setup(u => u.DownloadPackageOrNullAsync(It.IsAny<string>(), It.IsAny<NuGetVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream)null);

        _upstreamFactory
            .Setup(f => f.CreateForFeed(_feedWithMirror))
            .Returns(_upstreamClient.Object);

        _upstreamFactory
            .Setup(f => f.CreateForFeed(_feedWithoutMirror))
            .Returns(disabledClient.Object);
    }

    private PackageService BuildService(Feed currentFeed)
    {
        var feedService = new Mock<IFeedService>();
        feedService
            .Setup(s => s.GetFeedByIdAsync(currentFeed.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentFeed);

        return new PackageService(
            _db.Object,
            _upstreamFactory.Object,
            feedService.Object,
            _indexer.Object,
            Mock.Of<ILogger<PackageService>>());
    }

    [Fact]
    public async Task FeedWithMirrorEnabled_HitsUpstreamWhenPackageNotLocal()
    {
        var service = BuildService(_feedWithMirror);

        _db
            .Setup(d => d.ExistsAsync(_feedWithMirror.Id, _packageId, _packageVersion, _cancellation))
            .ReturnsAsync(false);

        using var stream = new MemoryStream();
        _upstreamClient
            .Setup(u => u.DownloadPackageOrNullAsync(_packageId, _packageVersion, _cancellation))
            .ReturnsAsync(stream);

        _indexer
            .Setup(i => i.IndexAsync(_feedWithMirror.Id, _feedWithMirror.Slug, stream, It.IsAny<string>(), _cancellation))
            .ReturnsAsync(PackageIndexingResult.Success);

        await service.ExistsAsync(_feedWithMirror.Id, _feedWithMirror.Slug, _packageId, _packageVersion, _cancellation);

        _upstreamClient.Verify(
            u => u.DownloadPackageOrNullAsync(_packageId, _packageVersion, _cancellation),
            Times.Once);
    }

    [Fact]
    public async Task FeedWithMirrorDisabled_DoesNotHitUpstream()
    {
        var service = BuildService(_feedWithoutMirror);

        _db
            .Setup(d => d.ExistsAsync(_feedWithoutMirror.Id, _packageId, _packageVersion, _cancellation))
            .ReturnsAsync(false);

        await service.ExistsAsync(_feedWithoutMirror.Id, _feedWithoutMirror.Slug, _packageId, _packageVersion, _cancellation);

        // The real upstream client (for the mirror-enabled feed) should never be called.
        _upstreamClient.Verify(
            u => u.DownloadPackageOrNullAsync(It.IsAny<string>(), It.IsAny<NuGetVersion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FeedWithMirrorEnabled_SkipsUpstreamIfPackageAlreadyLocal()
    {
        var service = BuildService(_feedWithMirror);

        _db
            .Setup(d => d.ExistsAsync(_feedWithMirror.Id, _packageId, _packageVersion, _cancellation))
            .ReturnsAsync(true);

        await service.ExistsAsync(_feedWithMirror.Id, _feedWithMirror.Slug, _packageId, _packageVersion, _cancellation);

        _upstreamClient.Verify(
            u => u.DownloadPackageOrNullAsync(It.IsAny<string>(), It.IsAny<NuGetVersion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
