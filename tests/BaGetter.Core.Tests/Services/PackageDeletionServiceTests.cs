using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Services;

public class PackageDeletionServiceTests
{
    private static readonly string PackageId = "Package";
    private static readonly NuGetVersion PackageVersion = new NuGetVersion("1.0.0");

    private readonly Mock<IPackageDatabase> _packages;
    private readonly Mock<IPackageStorageService> _storage;

    private readonly BaGetterOptions _options;
    private readonly PackageDeletionService _target;

    public PackageDeletionServiceTests()
    {
        _packages = new Mock<IPackageDatabase>();
        _storage = new Mock<IPackageStorageService>();
        _options = new BaGetterOptions();

        var optionsSnapshot = new Mock<IOptionsSnapshot<BaGetterOptions>>();
        optionsSnapshot.Setup(o => o.Value).Returns(_options);

        _target = new PackageDeletionService(
            _packages.Object,
            _storage.Object,
            optionsSnapshot.Object,
            Mock.Of<ILogger<PackageDeletionService>>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WhenUnlist_ReturnsTrueOnlyIfPackageExists(bool packageExists)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        _options.PackageDeletionBehavior = PackageDeletionBehavior.Unlist;

        _packages
            .Setup(p => p.UnlistPackageAsync(PackageId, PackageVersion, cancellationToken))
            .ReturnsAsync(packageExists);

        // Act
        var result = await _target.TryDeletePackageAsync(PackageId, PackageVersion, cancellationToken);

        // Assert
        Assert.Equal(packageExists, result);

        _packages.Verify(
            p => p.UnlistPackageAsync(PackageId, PackageVersion, cancellationToken),
            Times.Once);

        _packages.Verify(
            p => p.HardDeletePackageAsync(It.IsAny<string>(), It.IsAny<NuGetVersion>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _storage.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<NuGetVersion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WhenHardDelete_ReturnsTrueOnlyIfPackageExists(bool packageExists)
    {
        // Arrange
        _options.PackageDeletionBehavior = PackageDeletionBehavior.HardDelete;

        var step = 0;
        var databaseStep = -1;
        var storageStep = -1;
        var cancellationToken = CancellationToken.None;

        _packages
            .Setup(p => p.HardDeletePackageAsync(PackageId, PackageVersion, cancellationToken))
            .Callback(() => databaseStep = step++)
            .ReturnsAsync(packageExists);

        _storage
            .Setup(s => s.DeleteAsync(PackageId, PackageVersion, cancellationToken))
            .Callback(() => storageStep = step++)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _target.TryDeletePackageAsync(PackageId, PackageVersion, cancellationToken);

        // Assert - The database step MUST happen before the storage step.
        Assert.Equal(packageExists, result);
        Assert.Equal(0, databaseStep);
        Assert.Equal(1, storageStep);

        // The storage deletion should happen even if the package couldn't
        // be found in the database. This ensures consistency.
        _packages.Verify(
            p => p.HardDeletePackageAsync(PackageId, PackageVersion, cancellationToken),
            Times.Once);
        _storage.Verify(
            s => s.DeleteAsync(PackageId, PackageVersion, cancellationToken),
            Times.Once);

        _packages.Verify(
            p => p.UnlistPackageAsync(It.IsAny<string>(), It.IsAny<NuGetVersion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(2, 3)]
    [InlineData(1, 6)]
    public async Task WhenAddNewPackage_DeleteOldPackages_Major(uint maxVersions, int expectedCount)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var databaseStep = 0;
        var storageStep = 0;
        _packages
            .Setup(p => p.FindAsync(PackageId, true, cancellationToken))
            .ReturnsAsync([
                new Package { Id = PackageId, Version = new NuGetVersion("1.0.0-dev") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.0.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("2.0.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("2.1.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("2.1.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("3.0.0") },
            ]);
        _storage
            .Setup(s => s.DeleteAsync(PackageId, It.IsAny<NuGetVersion>(), cancellationToken))
            .Callback(() => storageStep++)
            .Returns(Task.CompletedTask);
        _packages
            .Setup(p => p.HardDeletePackageAsync(PackageId, It.IsAny<NuGetVersion>(), cancellationToken))
            .Callback(() => databaseStep++)
            .ReturnsAsync(true);

        // Act
        var deleted = await _target.DeleteOldVersionsAsync(
            new Package { Id = PackageId, Version = new NuGetVersion("4.0.0"), IsPrerelease = false },
            maxMajor: maxVersions, maxMinor:null, maxPatch:null, maxPrerelease:null, cancellationToken);

        // Assert - The database step MUST happen before the storage step.
        Assert.Equal(expectedCount, deleted);
        Assert.Equal(expectedCount, databaseStep);
        Assert.Equal(expectedCount, storageStep);
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(2, 5)]
    [InlineData(1, 7)]
    public async Task WhenAddNewPackage_DeleteOldPackages_Minor(uint maxVersions, int expectedCount)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var databaseStep = 0;
        var storageStep = 0;
        _packages
            .Setup(p => p.FindAsync(PackageId, true, cancellationToken))
            .ReturnsAsync([
                new Package { Id = PackageId, Version = new NuGetVersion("1.0.0-dev") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.0.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.0.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.2.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.2.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0") },
            ]);
        _storage
            .Setup(s => s.DeleteAsync(PackageId, It.IsAny<NuGetVersion>(), cancellationToken))
            .Callback(() => storageStep++)
            .Returns(Task.CompletedTask);
        _packages
            .Setup(p => p.HardDeletePackageAsync(PackageId, It.IsAny<NuGetVersion>(), cancellationToken))
            .Callback(() => databaseStep++)
            .ReturnsAsync(true);

        // Act
        var deleted = await _target.DeleteOldVersionsAsync(
            new Package { Id = PackageId, Version = new NuGetVersion("4.0.0"), IsPrerelease = false },
            maxMajor: null, maxMinor:maxVersions, maxPatch:null, maxPrerelease:null, cancellationToken);

        // Assert - The database step MUST happen before the storage step.
        Assert.Equal(expectedCount, deleted);
        Assert.Equal(expectedCount, databaseStep);
        Assert.Equal(expectedCount, storageStep);
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(2, 2)]
    [InlineData(1, 5)]
    public async Task WhenAddNewPackage_DeleteOldPackages_Patch(uint maxPrereleaseVersions, int expectedCount)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var databaseStep = 0;
        var storageStep = 0;
        _packages
            .Setup(p => p.FindAsync(PackageId, true, cancellationToken))
            .ReturnsAsync([
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-dev") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.3") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.5") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.2.0-beta.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.2.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.2.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0") },
            ]);
        _storage
            .Setup(s => s.DeleteAsync(PackageId, It.IsAny<NuGetVersion>(), cancellationToken))
            .Callback(() => storageStep++)
            .Returns(Task.CompletedTask);
        _packages
            .Setup(p => p.HardDeletePackageAsync(PackageId, It.IsAny<NuGetVersion>(), cancellationToken))
            .Callback(() => databaseStep++)
            .ReturnsAsync(true);

        // Act
        var deleted = await _target.DeleteOldVersionsAsync(
            new Package { Id = PackageId, Version = new NuGetVersion("4.0.0"), IsPrerelease = false },
            maxMajor: null, maxMinor:null, maxPatch:maxPrereleaseVersions, maxPrerelease:null, cancellationToken);

        // Assert - The database step MUST happen before the storage step.
        Assert.Equal(expectedCount, deleted);
        Assert.Equal(expectedCount, databaseStep);
        Assert.Equal(expectedCount, storageStep);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(4, 2)]
    [InlineData(3, 5)]
    [InlineData(2, 9)]
    [InlineData(1, 16)]
    [InlineData(0, 24)]
    public async Task WhenAddNewPackage_DeleteOldPackages_Prerelease(uint maxVersions, int expectedCount)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var databaseStep = 0;
        var storageStep = 0;
        _packages
            .Setup(p => p.FindAsync(PackageId, true, cancellationToken))
            .ReturnsAsync([
                new Package { Id = PackageId, Version = new NuGetVersion("1.0.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-beta.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-beta.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-beta.3") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-beta.4") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-beta.5") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-test.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-test.3") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-test.7") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-test.8") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-test.9") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-dev1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-dev2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-dev3") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-dev4") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.1-alpha.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.1-alpha.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.1-alpha.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.2.0-latest") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.2.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0-dev1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0-dev2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0-alpha.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0-alpha.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0-beta.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0-beta.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.3") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.4") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.5") },
            ]);
        _storage
            .Setup(s => s.DeleteAsync(PackageId, It.IsAny<NuGetVersion>(), cancellationToken))
            .Callback(() => storageStep++)
            .Returns(Task.CompletedTask);
        _packages
            .Setup(p => p.HardDeletePackageAsync(PackageId, It.IsAny<NuGetVersion>(), cancellationToken))
            .Callback(() => databaseStep++)
            .ReturnsAsync(true);

        // Act
        var deleted = await _target.DeleteOldVersionsAsync(
            new Package { Id = PackageId, Version = new NuGetVersion("4.0.0"), IsPrerelease = false },
            maxMajor: null, maxMinor:null, maxPatch:null, maxPrerelease:maxVersions, cancellationToken);

        // Assert - The database step MUST happen before the storage step.
        Assert.Equal(expectedCount, deleted);
        Assert.Equal(expectedCount, databaseStep);
        Assert.Equal(expectedCount, storageStep);
    }

    [Theory]
    [InlineData(4, 4, 6,5, 0)]
    [InlineData(4, 4, 6,4, 1)]
    [InlineData(4, 4, 6,1, 14)]
    [InlineData(4, 4, 3,1, 20)]
    [InlineData(4, 2, 3,1, 25)]
    [InlineData(1, 2, 3,1, 39)]
    [InlineData(4, 4, 4,5, 8)]
    [InlineData(4, 1, 4,5, 30)]
    public async Task WhenAddNewPackage_DeleteOldPackages_CrossLimitsCheck(uint maxMajorVersions,uint maxMinorVersions,uint maxPatchVersions,uint maxPrereleaseVersions, int expectedCount)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var databaseStep = 0;
        var storageStep = 0;
        _packages
            .Setup(p => p.FindAsync(PackageId, true, cancellationToken))
            .ReturnsAsync([
                new Package { Id = PackageId, Version = new NuGetVersion("1.0.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-beta.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-beta.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-beta.3") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-beta.4") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-beta.5") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-test.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-dev1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-dev2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.0-dev3") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.1-alpha.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.1-alpha.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.1.1-alpha.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.2.0-latest") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.2.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.2.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0-dev1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0-dev2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0-alpha.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0-alpha.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0-beta.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0-beta.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.3") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.4") },
                new Package { Id = PackageId, Version = new NuGetVersion("1.3.5") },
                new Package { Id = PackageId, Version = new NuGetVersion("2.3.0-dev1") },
                new Package { Id = PackageId, Version = new NuGetVersion("2.3.0-dev2") },
                new Package { Id = PackageId, Version = new NuGetVersion("2.3.0-alpha.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("2.3.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("2.3.1-alpha.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("2.3.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("3.1.0-beta.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("3.1.0-beta.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("3.1.0") },
                new Package { Id = PackageId, Version = new NuGetVersion("3.3.0-dev1") },
                new Package { Id = PackageId, Version = new NuGetVersion("3.3.0-dev2") },
                new Package { Id = PackageId, Version = new NuGetVersion("4.0.1") },
                new Package { Id = PackageId, Version = new NuGetVersion("4.0.2") },
                new Package { Id = PackageId, Version = new NuGetVersion("4.0.3") },
                new Package { Id = PackageId, Version = new NuGetVersion("4.4.4") },
            ]);
        _storage
            .Setup(s => s.DeleteAsync(PackageId, It.IsAny<NuGetVersion>(), cancellationToken))
            .Callback(() => storageStep++)
            .Returns(Task.CompletedTask);
        _packages
            .Setup(p => p.HardDeletePackageAsync(PackageId, It.IsAny<NuGetVersion>(), cancellationToken))
            .Callback(() => databaseStep++)
            .ReturnsAsync(true);

        // Act
        var deleted = await _target.DeleteOldVersionsAsync(
            new Package { Id = PackageId, Version = new NuGetVersion("4.4.5"), IsPrerelease = false },
            maxMajor: maxMajorVersions, maxMinor:maxMinorVersions, maxPatch:maxPatchVersions, maxPrerelease:maxPrereleaseVersions, cancellationToken);

        // Assert - The database step MUST happen before the storage step.
        Assert.Equal(expectedCount, deleted);
        Assert.Equal(expectedCount, databaseStep);
        Assert.Equal(expectedCount, storageStep);
    }
}
