using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Filtering;

public class PackageFilteringPackageServiceTests
{
    private static readonly CancellationToken CancellationToken = CancellationToken.None;

    [Fact]
    public async Task DisabledFilteringDelegatesWithoutDatabaseLookup()
    {
        var version = NuGetVersion.Parse("1.0.0");
        var inner = new Mock<IPackageService>();
        inner
            .Setup(service => service.ExistsAsync("Example.Package", version, CancellationToken))
            .ReturnsAsync(true);
        var packages = new Mock<IPackageDatabase>(MockBehavior.Strict);
        var policy = new Mock<IPackagePolicyEvaluator>();
        policy.SetupGet(evaluator => evaluator.IsFilteringEnabled).Returns(false);
        var target = new PackageFilteringPackageService(inner.Object, packages.Object, policy.Object);

        var result = await target.ExistsAsync("Example.Package", version, CancellationToken);

        Assert.True(result);
        packages.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BlockedUpstreamDownloadDoesNotCallInnerService()
    {
        var version = NuGetVersion.Parse("1.0.0");
        var inner = new Mock<IPackageService>(MockBehavior.Strict);
        var packages = PackageDatabase();
        var policy = Policy(context =>
            context.Scope == PackageFilterScope.Upstream &&
            context.PackageId == "Example.Package" &&
            context.Version == version);
        var target = new PackageFilteringPackageService(inner.Object, packages.Object, policy.Object);

        var result = await target.ExistsAsync("Example.Package", version, CancellationToken);

        Assert.False(result);
        inner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BlockedCachedPackageDoesNotCallInnerService()
    {
        var version = NuGetVersion.Parse("1.0.0");
        var inner = new Mock<IPackageService>(MockBehavior.Strict);
        var packages = PackageDatabase(CachedPackage("Example.Package", "1.0.0"));
        var policy = Policy(context => context.Scope == PackageFilterScope.CachedUpstream);
        var target = new PackageFilteringPackageService(inner.Object, packages.Object, policy.Object);

        var result = await target.FindPackageOrNullAsync("Example.Package", version, CancellationToken);

        Assert.Null(result);
        inner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task VersionListFiltersUpstreamVersionsButKeepsLocalVersions()
    {
        var inner = new Mock<IPackageService>();
        inner
            .Setup(service => service.FindPackageVersionsAsync("Example.Package", CancellationToken))
            .ReturnsAsync(new[]
            {
                NuGetVersion.Parse("1.0.0"),
                NuGetVersion.Parse("2.0.0"),
            });
        var packages = PackageDatabase(LocalPackage("Example.Package", "1.0.0"));
        var policy = Policy(context =>
            context.Scope == PackageFilterScope.Upstream &&
            context.Version == NuGetVersion.Parse("2.0.0"));
        var target = new PackageFilteringPackageService(inner.Object, packages.Object, policy.Object);

        var result = await target.FindPackageVersionsAsync("Example.Package", CancellationToken);

        var version = Assert.Single(result);
        Assert.Equal("1.0.0", version.ToNormalizedString());
    }

    [Fact]
    public async Task MetadataFiltersCachedAndUpstreamPackages()
    {
        var inner = new Mock<IPackageService>();
        inner
            .Setup(service => service.FindPackagesAsync("Example.Package", CancellationToken))
            .ReturnsAsync(new[]
            {
                CachedPackage("Example.Package", "1.0.0"),
                LocalPackage("Example.Package", "2.0.0"),
                UpstreamPackage("Example.Package", "3.0.0"),
            });
        var packages = PackageDatabase(
            CachedPackage("Example.Package", "1.0.0"),
            LocalPackage("Example.Package", "2.0.0"));
        var policy = Policy(context =>
            context.Scope == PackageFilterScope.CachedUpstream ||
            context.Scope == PackageFilterScope.Upstream);
        var target = new PackageFilteringPackageService(inner.Object, packages.Object, policy.Object);

        var result = await target.FindPackagesAsync("Example.Package", CancellationToken);

        var package = Assert.Single(result);
        Assert.Equal("2.0.0", package.Version.ToNormalizedString());
    }

    [Fact]
    public async Task AddDownloadDelegatesToInnerService()
    {
        var version = NuGetVersion.Parse("1.0.0");
        var inner = new Mock<IPackageService>();
        var packages = new Mock<IPackageDatabase>(MockBehavior.Strict);
        var policy = new Mock<IPackagePolicyEvaluator>();
        var target = new PackageFilteringPackageService(inner.Object, packages.Object, policy.Object);

        await target.AddDownloadAsync("Example.Package", version, CancellationToken);

        inner.Verify(
            service => service.AddDownloadAsync("Example.Package", version, CancellationToken),
            Times.Once);
        packages.VerifyNoOtherCalls();
    }

    private static Mock<IPackageDatabase> PackageDatabase(params Package[] packages)
    {
        var packagesByVersion = new Dictionary<SemanticVersion, Package>(VersionComparer.VersionReleaseMetadata);
        foreach (var package in packages)
        {
            packagesByVersion[package.Version] = package;
        }

        var database = new Mock<IPackageDatabase>();
        database
            .Setup(service => service.FindAsync(
                It.IsAny<string>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(packages);
        database
            .Setup(service => service.FindOrNullAsync(
                It.IsAny<string>(),
                It.IsAny<NuGetVersion>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, NuGetVersion version, bool _, CancellationToken _) =>
                packagesByVersion.GetValueOrDefault(version));
        return database;
    }

    private static Mock<IPackagePolicyEvaluator> Policy(
        Func<PackageFilterContext, bool> isBlocked)
    {
        var policy = new Mock<IPackagePolicyEvaluator>();
        policy.SetupGet(evaluator => evaluator.IsFilteringEnabled).Returns(true);
        policy
            .Setup(evaluator => evaluator.Evaluate(It.IsAny<PackageFilterContext>()))
            .Returns((PackageFilterContext context) => isBlocked(context)
                ? PackageFilterDecision.Block(new PackageFilterRuleOptions
                {
                    PackageId = "Example.*",
                    Versions = "*"
                })
                : PackageFilterDecision.Allow);
        return policy;
    }

    private static Package CachedPackage(string packageId, string version)
    {
        var package = LocalPackage(packageId, version);
        package.CachedFrom = "https://packages.example/v3/index.json";
        return package;
    }

    private static Package LocalPackage(string packageId, string version)
        => new()
        {
            Id = packageId,
            Version = NuGetVersion.Parse(version),
            Listed = true,
        };

    private static Package UpstreamPackage(string packageId, string version)
        => new()
        {
            Id = packageId,
            Version = NuGetVersion.Parse(version),
        };
}
