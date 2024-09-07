using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BaGetter.Core.Tests.Support;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NuGet.Packaging;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Services;

/// <summary>
/// These tests are similar to the ones in <see cref="PackageIndexingServiceTests"/>, but they use an in-memory package database.
/// </summary>
public class PackageIndexingServiceInMemoryTests
{
    private readonly IPackageDatabase _packages;
    private readonly Mock<IPackageStorageService> _storage;
    private readonly Mock<ISearchIndexer> _search;
    private readonly IPackageDeletionService _deleter;
    private readonly Mock<SystemTime> _time;
    private readonly PackageIndexingService _target;
    private readonly BaGetterOptions _options;

    public PackageIndexingServiceInMemoryTests()
    {
        _packages = new InMemoryPackageDatabase();
        _storage = new Mock<IPackageStorageService>(MockBehavior.Strict);
        _search = new Mock<ISearchIndexer>(MockBehavior.Strict);
        _options = new();

        var optionsSnapshot = new Mock<IOptionsSnapshot<BaGetterOptions>>();
        optionsSnapshot.Setup(o => o.Value).Returns(_options);

        _deleter = new PackageDeletionService(
            _packages,
            _storage.Object,
            optionsSnapshot.Object,
            Mock.Of<ILogger<PackageDeletionService>>());
        _time = new Mock<SystemTime>(MockBehavior.Loose);
        var options = new Mock<IOptionsSnapshot<BaGetterOptions>>(MockBehavior.Strict);
        options.Setup(o => o.Value).Returns(_options);

        _target = new PackageIndexingService(
            _packages,
            _storage.Object,
            _deleter,
            _search.Object,
            _time.Object,
            options.Object,
            Mock.Of<ILogger<PackageIndexingService>>());
    }

    // TODO: Add malformed package tests

    [Fact]
    public async Task IndexIMAsync_WhenPackageAlreadyExists_AndOverwriteForbidden_ReturnsPackageAlreadyExists()
    {
        // Arrange
        _options.AllowPackageOverwrites = PackageOverwriteAllowed.False;

        var builder = new PackageBuilder
        {
            Id = "bagetter-test",
            Version = NuGetVersion.Parse("1.0.0"),
            Description = "Test Description",
        };
        builder.Authors.Add("Test Author");
        var assemblyFile = GetType().Assembly.Location;
        builder.Files.Add(new PhysicalPackageFile
        {
            SourcePath = assemblyFile,
            TargetPath = "lib/Test.dll"
        });
        var stream = new MemoryStream();
        builder.Save(stream);
        _storage.Setup(s => s.SavePackageContentAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), stream, It.IsAny<FileStream>(), default, default, default)).Returns(Task.CompletedTask);
        _search.Setup(s => s.IndexAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), default)).Returns(Task.CompletedTask);

        // Act
        var result = await _target.IndexAsync(stream, default);

        var stream2 = new MemoryStream();
        builder.Save(stream);

        Assert.Equal(PackageIndexingResult.Success, result);

        // Act
        var result2 = await _target.IndexAsync(stream, default);

        // Assert
        Assert.Equal(PackageIndexingResult.PackageAlreadyExists, result2);

    }

    [Fact]
    public async Task IndexIMAsync_WhenPackageAlreadyExists_AndOverwriteAllowed_IndexesPackage()
    {
        // Arrange
        _options.AllowPackageOverwrites = PackageOverwriteAllowed.True;

        var builder = new PackageBuilder
        {
            Id = "bagetter-test",
            Version = NuGetVersion.Parse("1.0.0"),
            Description = "Test Description",
        };
        builder.Authors.Add("Test Author");
        var assemblyFile = GetType().Assembly.Location;
        builder.Files.Add(new PhysicalPackageFile
        {
            SourcePath = assemblyFile,
            TargetPath = "lib/Test.dll"
        });
        var stream = new MemoryStream();
        builder.Save(stream);

        _storage.Setup(s => s.DeleteAsync(builder.Id, builder.Version, default)).Returns(Task.CompletedTask);
        _storage.Setup(s => s.SavePackageContentAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), stream, It.IsAny<FileStream>(), default, default, default)).Returns(Task.CompletedTask);

        _search.Setup(s => s.IndexAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), default)).Returns(Task.CompletedTask);

        // Act
        var result = await _target.IndexAsync(stream, default);

        // Assert
        Assert.Equal(PackageIndexingResult.Success, result);
    }

    [Fact]
    public async Task IndexIMAsync_WhenPrereleasePackageAlreadyExists_AndOverwritePrereleaseAllowed_IndexesPackage()
    {
        // Arrange
        _options.AllowPackageOverwrites = PackageOverwriteAllowed.PrereleaseOnly;

        var builder = new PackageBuilder
        {
            Id = "bagetter-test",
            Version = NuGetVersion.Parse("1.0.0-beta"),
            Description = "Test Description",
        };
        builder.Authors.Add("Test Author");
        var assemblyFile = GetType().Assembly.Location;
        builder.Files.Add(new PhysicalPackageFile
        {
            SourcePath = assemblyFile,
            TargetPath = "lib/Test.dll"
        });
        var stream = new MemoryStream();
        builder.Save(stream);

        _storage.Setup(s => s.DeleteAsync(builder.Id, builder.Version, default)).Returns(Task.CompletedTask);
        _storage.Setup(s => s.SavePackageContentAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), stream, It.IsAny<FileStream>(), default, default, default)).Returns(Task.CompletedTask);

        _search.Setup(s => s.IndexAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), default)).Returns(Task.CompletedTask);

        // Act
        var result = await _target.IndexAsync(stream, default);

        // Assert
        Assert.Equal(PackageIndexingResult.Success, result);
    }

    [Fact]
    public async Task IndexIMAsync_WhenPrereleasePackageAlreadyExists_AndOverwriteForbidden_ReturnsPackageAlreadyExists()
    {
        // Arrange
        _options.AllowPackageOverwrites = PackageOverwriteAllowed.False;

        var builder = new PackageBuilder
        {
            Id = "bagetter-test",
            Version = NuGetVersion.Parse("1.0.0-beta"),
            Description = "Test Description",
        };
        builder.Authors.Add("Test Author");
        var assemblyFile = GetType().Assembly.Location;
        builder.Files.Add(new PhysicalPackageFile
        {
            SourcePath = assemblyFile,
            TargetPath = "lib/Test.dll"
        });
        var stream = new MemoryStream();
        builder.Save(stream);

        _storage.Setup(s => s.DeleteAsync(builder.Id, builder.Version, default)).Returns(Task.CompletedTask);
        _storage.Setup(s => s.SavePackageContentAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), stream, It.IsAny<FileStream>(), default, default, default)).Returns(Task.CompletedTask);

        _search.Setup(s => s.IndexAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), default)).Returns(Task.CompletedTask);

        // Act
        var result = await _target.IndexAsync(stream, default);

        var stream2 = new MemoryStream();
        builder.Save(stream);

        Assert.Equal(PackageIndexingResult.Success, result);

        // Act
        var result2 = await _target.IndexAsync(stream, default);

        // Assert
        Assert.Equal(PackageIndexingResult.PackageAlreadyExists, result2);
    }

    [Fact]
    public async Task IndexIMAsync_WithValidPackage_ReturnsSuccess()
    {
        // Arrange
        _options.AllowPackageOverwrites = PackageOverwriteAllowed.False;
        var builder = new PackageBuilder
        {
            Id = "bagetter-test",
            Version = NuGetVersion.Parse("1.0.0"),
            Description = "Test Description",
        };
        builder.Authors.Add("Test Author");
        var assemblyFile = GetType().Assembly.Location;
        builder.Files.Add(new PhysicalPackageFile
        {
            SourcePath = assemblyFile,
            TargetPath = "lib/Test.dll"
        });
        var stream = new MemoryStream();
        builder.Save(stream);

        _storage.Setup(s => s.SavePackageContentAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), stream, It.IsAny<FileStream>(), default, default, default)).Returns(Task.CompletedTask);

        _search.Setup(s => s.IndexAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), default)).Returns(Task.CompletedTask);

        // Act
        var result = await _target.IndexAsync(stream, default);

        // Assert
        Assert.Equal(PackageIndexingResult.Success, result);
    }

    [Fact]
    public async Task IndexIMAsync_WithValidPackage_CleansOldVersions()
    {
        // Arrange
        _options.AllowPackageOverwrites = PackageOverwriteAllowed.False;
        _options.MaxVersionsPerPackage = 5;
        // Add 10 packages
        for (var i = 0; i < 10; i++)
        {
            var builder = new PackageBuilder
            {
                Id = "bagetter-test",
                Version = NuGetVersion.Parse($"1.0.{i}"),
                Description = "Test Description",
            };
            builder.Authors.Add("Test Author");
            var assemblyFile = GetType().Assembly.Location;
            builder.Files.Add(new PhysicalPackageFile
            {
                SourcePath = assemblyFile,
                TargetPath = "lib/Test.dll"
            });
            var stream = new MemoryStream();
            builder.Save(stream);
            //_packages.Setup(p => p.ExistsAsync(builder.Id, builder.Version, default)).ReturnsAsync(false);
            //_packages.Setup(p => p.AddAsync(It.Is<Package>(p1 => p1.Id == builder.Id && p1.Version.ToString() == builder.Version.ToString()), default)).ReturnsAsync(PackageAddResult.Success);

            _storage.Setup(s => s.SavePackageContentAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), stream, It.IsAny<FileStream>(), default, default, default)).Returns(Task.CompletedTask);

            _search.Setup(s => s.IndexAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), default)).Returns(Task.CompletedTask);

            // Act
            var result = await _target.IndexAsync(stream, default);

            // Assert
            Assert.Equal(PackageIndexingResult.Success, result);

            var packageCount = await _packages.FindAsync(builder.Id, true, default);
            if (i < 5)
            {
                Assert.Equal(i + 1, packageCount.Count);
            }
            else
            {
                Assert.Equal(5, packageCount.Count);
            }
        }
    }

}
