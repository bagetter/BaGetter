using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;
using BaGetter.Core.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Services;

/// <summary>
/// Verifies the storage path layout for named feeds vs. the default feed,
/// and that the default-feed legacy fallback works while named feeds do not
/// consult legacy unscoped paths.
/// </summary>
public class PackageStoragePathTests
{
    private const string PackageId = "Test.Package";
    private const string PackageVersion = "1.0.0";

    // Scoped paths (multi-feed layout)
    private static readonly string _namedFeedPackagePath =
        Path.Combine("packages", "internal", "test.package", "1.0.0", "test.package.1.0.0.nupkg");
    private static readonly string _defaultFeedPackagePath =
        Path.Combine("packages", "default", "test.package", "1.0.0", "test.package.1.0.0.nupkg");

    // Legacy (unscoped) path — written by pre-multi-feed BaGetter for the default feed
    private static readonly string _legacyPackagePath =
        Path.Combine("packages", "test.package", "1.0.0", "test.package.1.0.0.nupkg");

    private readonly Package _package = new Package
    {
        Id = PackageId,
        Version = new NuGetVersion(PackageVersion)
    };

    private readonly Mock<IStorageService> _storage;
    private readonly PackageStorageService _target;

    public PackageStoragePathTests()
    {
        _storage = new Mock<IStorageService>(MockBehavior.Strict);
        _target = new PackageStorageService(
            _storage.Object,
            Mock.Of<ILogger<PackageStorageService>>());
    }

    [Fact]
    public async Task NamedFeed_SavesPackageUnderFeedSlugPrefix()
    {
        _storage
            .Setup(s => s.PutAsync(
                It.Is<string>(p => p.StartsWith(Path.Combine("packages", "internal"))),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(StoragePutResult.Success);

        using var pkg = Stream.Null;
        using var nuspec = Stream.Null;
        await _target.SavePackageContentAsync(
            "internal", _package, pkg, nuspec, null, null);

        _storage.Verify(
            s => s.PutAsync(_namedFeedPackagePath, It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once,
            $"Expected package at '{_namedFeedPackagePath}'");
    }

    [Fact]
    public async Task DefaultFeed_SavesPackageUnderDefaultSlugPrefix()
    {
        _storage
            .Setup(s => s.PutAsync(
                It.Is<string>(p => p.StartsWith(Path.Combine("packages", "default"))),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(StoragePutResult.Success);

        using var pkg = Stream.Null;
        using var nuspec = Stream.Null;
        await _target.SavePackageContentAsync(
            "default", _package, pkg, nuspec, null, null);

        _storage.Verify(
            s => s.PutAsync(_defaultFeedPackagePath, It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once,
            $"Expected package at '{_defaultFeedPackagePath}'");
    }

    [Fact]
    public async Task DefaultFeed_FallsBackToLegacyPath_WhenScopedPathMissing()
    {
        // Scoped path not found — simulate pre-upgrade layout
        _storage
            .Setup(s => s.GetAsync(_defaultFeedPackagePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DirectoryNotFoundException());

        using var legacyStream = new MemoryStream(Encoding.UTF8.GetBytes("legacy-nupkg"));
        _storage
            .Setup(s => s.GetAsync(_legacyPackagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(legacyStream);

        var result = await _target.GetPackageStreamAsync(
            "default", PackageId, new NuGetVersion(PackageVersion), CancellationToken.None);

        Assert.NotNull(result);
        // Verify the legacy path was consulted
        _storage.Verify(s => s.GetAsync(_legacyPackagePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NamedFeed_DoesNotFallBackToLegacyPath_WhenScopedPathMissing()
    {
        // Scoped path not found for a named feed
        _storage
            .Setup(s => s.GetAsync(_namedFeedPackagePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DirectoryNotFoundException());

        // The legacy (unscoped) path must never be consulted for named feeds
        _storage
            .Setup(s => s.GetAsync(_legacyPackagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Stream.Null);  // would succeed if called

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _target.GetPackageStreamAsync(
                "internal", PackageId, new NuGetVersion(PackageVersion), CancellationToken.None));

        _storage.Verify(
            s => s.GetAsync(_legacyPackagePath, It.IsAny<CancellationToken>()),
            Times.Never,
            "Named feeds must not fall back to legacy unscoped paths");
    }
}
