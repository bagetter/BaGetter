using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private readonly IPackageStorageService _storage;
    private readonly Mock<ISearchIndexer> _search;
    private readonly IPackageDeletionService _deleter;
    private readonly Mock<SystemTime> _time;
    private readonly PackageIndexingService _target;
    private readonly BaGetterOptions _options;
    private readonly RetentionOptions _retentionOptions;

    public PackageIndexingServiceInMemoryTests()
    {
        _packages = new InMemoryPackageDatabase();
        var storageService = new NullStorageService();
        _storage = new PackageStorageService(storageService, Mock.Of<ILogger<PackageStorageService>>());

        _search = new Mock<ISearchIndexer>(MockBehavior.Strict);
        _options = new();
        _retentionOptions = new();

        var optionsSnapshot = new Mock<IOptionsSnapshot<BaGetterOptions>>();
        optionsSnapshot.Setup(o => o.Value).Returns(_options);

        _deleter = new PackageDeletionService(
            _packages,
            _storage,
            optionsSnapshot.Object,
            Mock.Of<ILogger<PackageDeletionService>>());
        _time = new Mock<SystemTime>(MockBehavior.Loose);
        var options = new Mock<IOptionsSnapshot<BaGetterOptions>>(MockBehavior.Strict);
        options.Setup(o => o.Value).Returns(_options);
        var retentionOptions = new Mock<IOptionsSnapshot<RetentionOptions>>(MockBehavior.Strict);
        retentionOptions.Setup(o => o.Value).Returns(_retentionOptions);

        _target = new PackageIndexingService(
            _packages,
            _storage,
            _deleter,
            _search.Object,
            _time.Object,
            options.Object,
            retentionOptions.Object,
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

        _retentionOptions.MaxMajorVersions = 2;
        _retentionOptions.MaxMinorVersions = 2;
        _retentionOptions.MaxPatchVersions = 5;
        _retentionOptions.MaxPrereleaseVersions = 5;
        // Add 10 packages
        for (var major = 1; major < 4; major++)
        {
            for (var minor = 1; minor < 4; minor++)
            {
                for (var patch = 1; patch < 7; patch++)
                {
                    await StoreVersion(NuGetVersion.Parse($"{major}.{minor}.{patch}"));
                    for (var prerelease = 1; prerelease < 7; prerelease++)
                    {
                        await StoreVersion(NuGetVersion.Parse($"{major}.{minor}.{patch}-staging.{prerelease}"));

                        var version = NuGetVersion.Parse($"{major}.{minor}.{patch}-beta.{prerelease}");

                        var builder = await StoreVersion(version);

                        var packageVersions = await _packages.FindAsync(builder.Id, true, default);
                        var majorCount = packageVersions.Select(p => p.Version.Major).Distinct().Count();
                        Assert.Equal(majorCount, Math.Min(major, (int)_retentionOptions.MaxMajorVersions));
                        Assert.True(majorCount <= _retentionOptions.MaxMajorVersions, $"Major version {major} has {majorCount} packages");

                        // validate maximum number of minor versions for each major version.
                        var minorVersions = packageVersions.GroupBy(m => m.Version.Major)
                            .Select(gp => (version: gp.Key, versionCount: gp.Select(p => p.Version.Major + "." + p.Version.Minor).Distinct().Count())).ToList();
                        Assert.All(minorVersions, g => Assert.True(g.versionCount <= _retentionOptions.MaxMinorVersions, $"Minor version {g.version} has {g.versionCount} packages"));

                        // validate maximum number of minor versions for each major version.
                        var patches = packageVersions.GroupBy(m => (m.Version.Major, m.Version.Minor))
                            .Select(gp => (version: gp.Key, versionCount: gp.Select(p => p.Version.Major + "." + p.Version.Minor + "." + p.Version.Patch).Distinct().Count())).ToList();
                        Assert.All(patches, g => Assert.True(g.versionCount <= _retentionOptions.MaxPatchVersions, $"Patch version {g.version} has {g.versionCount} packages"));

                        // validate maximum number of beta versions for each major,minor,patch version.
                        var betaVersions = packageVersions.Where(p => p.IsPrerelease && p.Version.ReleaseLabels.First() == "beta")
                            .GroupBy(m => (m.Version.Major, m.Version.Minor, m.Version.Patch))
                            .Select(gp => (version: gp.Key, versionCount: gp.Select(p => p.Version.Major + "." + p.Version.Minor + "." + p.Version.Patch).Distinct().Count())).ToList();
                        Assert.All(betaVersions, g => Assert.True(g.versionCount <= _retentionOptions.MaxPatchVersions, $"Pre-Release version {g.version} has {g.versionCount} packages"));


                    }
                }
            }

        }
    }

    private async Task<PackageBuilder> StoreVersion(NuGetVersion version)
        {
            var builder = new PackageBuilder
            {
                Id = "bagetter-test",
            Version = version,
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

            _search.Setup(s => s.IndexAsync(It.Is<Package>(p => p.Id == builder.Id && p.Version.ToString() == builder.Version.ToString()), default)).Returns(Task.CompletedTask);

            // Act
            var result = await _target.IndexAsync(stream, default);

            // Assert
            Assert.Equal(PackageIndexingResult.Success, result);
        return builder;
    }

}
