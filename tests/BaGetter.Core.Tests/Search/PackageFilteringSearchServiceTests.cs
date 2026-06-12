using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Tests.Support;
using BaGetter.Protocol.Models;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Search;

public class PackageFilteringSearchServiceTests
{
    private static readonly CancellationToken CancellationToken = CancellationToken.None;

    [Fact]
    public async Task DisabledFilteringReturnsExistingSearchResponse()
    {
        var request = new SearchRequest { Take = 20 };
        var expected = new SearchResponse();
        var inner = new Mock<ISearchService>();
        inner.Setup(service => service.SearchAsync(request, CancellationToken)).ReturnsAsync(expected);
        var packages = new Mock<IPackageDatabase>(MockBehavior.Strict);
        var policy = new Mock<IPackagePolicyEvaluator>();
        policy.SetupGet(evaluator => evaluator.IsFilteringEnabled).Returns(false);
        var target = CreateTarget(inner.Object, packages.Object, policy.Object);

        var result = await target.SearchAsync(request, CancellationToken);

        Assert.Same(expected, result);
        packages.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchRemovesBlockedCachedVersions()
    {
        const string packageId = "Example.Package";
        var allowedPackage = CachedPackage(packageId, "1.0.0");
        var blockedPackage = CachedPackage(packageId, "2.0.0");
        var inner = SearchServiceWithResults(
            SearchResult(packageId, "1.0.0", "2.0.0"));
        var packages = PackageDatabase(
            (packageId, new[] { allowedPackage, blockedPackage }));
        var policy = Policy(context => context.Version >= NuGetVersion.Parse("2.0.0"));
        var target = CreateTarget(inner.Object, packages.Object, policy.Object);

        var result = await target.SearchAsync(
            new SearchRequest { Take = 20 },
            CancellationToken);

        var package = Assert.Single(result.Data);
        Assert.Equal("1.0.0", package.Version);
        Assert.Equal("1.0.0", Assert.Single(package.Versions).Version);
    }

    [Fact]
    public async Task SearchKeepsAllowedUpstreamOnlyPackages()
    {
        const string packageId = "Example.UpstreamOnly";
        var inner = SearchServiceWithResults(SearchResult(packageId, "1.0.0"));
        var packages = PackageDatabase();
        var policy = Policy(_ => false);
        var target = CreateTarget(inner.Object, packages.Object, policy.Object);

        var result = await target.SearchAsync(
            new SearchRequest { Take = 20 },
            CancellationToken);

        Assert.Equal(packageId, Assert.Single(result.Data).PackageId);
    }

    [Fact]
    public async Task SearchRemovesBlockedUpstreamOnlyVersions()
    {
        const string packageId = "Example.UpstreamOnly";
        var inner = SearchServiceWithResults(SearchResult(packageId, "1.0.0", "2.0.0"));
        var packages = PackageDatabase();
        var policy = Policy(context =>
            context.Scope == PackageFilterScope.Upstream &&
            context.Version >= NuGetVersion.Parse("2.0.0"));
        var target = CreateTarget(inner.Object, packages.Object, policy.Object);

        var result = await target.SearchAsync(
            new SearchRequest { Take = 20 },
            CancellationToken);

        var package = Assert.Single(result.Data);
        Assert.Equal("1.0.0", package.Version);
        Assert.Equal("1.0.0", Assert.Single(package.Versions).Version);
    }

    [Fact]
    public async Task SearchFillsPageAfterBlockedCachedPackage()
    {
        const string allowedId = "Example.ZAllowed";
        var blockedIds = Enumerable.Range(0, 20)
            .Select(index => $"Example.Blocked.{index:D2}")
            .ToList();
        var searchResults = blockedIds
            .Select(packageId => SearchResult(packageId, "1.0.0"))
            .Append(SearchResult(allowedId, "1.0.0"))
            .ToList();
        var packageData = blockedIds
            .Select(packageId => (
                packageId,
                new[] { CachedPackage(packageId, "1.0.0") }))
            .Append((
                allowedId,
                new[] { LocalPackage(allowedId, "1.0.0") }))
            .ToArray();
        var inner = SearchServiceWithPagedResults(searchResults);
        var packages = PackageDatabase(packageData);
        var policy = Policy(context => context.Scope == PackageFilterScope.CachedUpstream);
        var target = CreateTarget(inner.Object, packages.Object, policy.Object);

        var result = await target.SearchAsync(
            new SearchRequest { Take = 1 },
            CancellationToken);

        Assert.Equal(allowedId, Assert.Single(result.Data).PackageId);
        inner.Verify(
            service => service.SearchAsync(
                It.IsAny<SearchRequest>(),
                CancellationToken),
            Times.Exactly(2));
    }

    [Fact]
    public async Task AutocompleteRemovesBlockedCachedPackages()
    {
        const string blockedId = "Example.ABlocked";
        const string allowedId = "Example.ZAllowed";
        var inner = new Mock<ISearchService>();
        inner
            .Setup(service => service.AutocompleteAsync(
                It.IsAny<AutocompleteRequest>(),
                CancellationToken))
            .ReturnsAsync(new AutocompleteResponse
            {
                Data = new[] { blockedId, allowedId }
            });
        var packages = PackageDatabase(
            (blockedId, new[] { CachedPackage(blockedId, "1.0.0") }),
            (allowedId, new[] { LocalPackage(allowedId, "1.0.0") }));
        var policy = Policy(context => context.Scope == PackageFilterScope.CachedUpstream);
        var target = CreateTarget(inner.Object, packages.Object, policy.Object);

        var result = await target.AutocompleteAsync(
            new AutocompleteRequest { Take = 20 },
            CancellationToken);

        Assert.Equal(allowedId, Assert.Single(result.Data));
    }

    [Fact]
    public async Task AutocompleteKeepsAllowedUpstreamOnlyPackages()
    {
        const string packageId = "Example.UpstreamOnly";
        var inner = new Mock<ISearchService>();
        inner
            .Setup(service => service.AutocompleteAsync(
                It.IsAny<AutocompleteRequest>(),
                CancellationToken))
            .ReturnsAsync(new AutocompleteResponse
            {
                Data = new[] { packageId }
            });
        var packages = PackageDatabase();
        var policy = Policy(_ => false);
        var target = CreateTarget(inner.Object, packages.Object, policy.Object);

        var result = await target.AutocompleteAsync(
            new AutocompleteRequest { Take = 20 },
            CancellationToken);

        Assert.Equal(packageId, Assert.Single(result.Data));
    }

    [Fact]
    public async Task VersionListRemovesBlockedCachedVersions()
    {
        const string packageId = "Example.Package";
        var inner = new Mock<ISearchService>();
        inner
            .Setup(service => service.ListPackageVersionsAsync(
                It.IsAny<VersionsRequest>(),
                CancellationToken))
            .ReturnsAsync(new AutocompleteResponse
            {
                Data = new[] { "1.0.0", "2.0.0" }
            });
        var packages = PackageDatabase(
            (packageId, new[]
            {
                CachedPackage(packageId, "1.0.0"),
                CachedPackage(packageId, "2.0.0")
            }));
        var policy = Policy(context => context.Version >= NuGetVersion.Parse("2.0.0"));
        var target = CreateTarget(inner.Object, packages.Object, policy.Object);

        var result = await target.ListPackageVersionsAsync(
            new VersionsRequest { PackageId = packageId },
            CancellationToken);

        Assert.Equal("1.0.0", Assert.Single(result.Data));
    }

    [Fact]
    public async Task VersionListKeepsAllowedUpstreamOnlyVersions()
    {
        const string packageId = "Example.UpstreamOnly";
        var inner = new Mock<ISearchService>();
        inner
            .Setup(service => service.ListPackageVersionsAsync(
                It.IsAny<VersionsRequest>(),
                CancellationToken))
            .ReturnsAsync(new AutocompleteResponse
            {
                Data = new[] { "1.0.0", "2.0.0" }
            });
        var packages = PackageDatabase();
        var policy = Policy(context =>
            context.Scope == PackageFilterScope.Upstream &&
            context.Version >= NuGetVersion.Parse("2.0.0"));
        var target = CreateTarget(inner.Object, packages.Object, policy.Object);

        var result = await target.ListPackageVersionsAsync(
            new VersionsRequest { PackageId = packageId },
            CancellationToken);

        Assert.Equal("1.0.0", Assert.Single(result.Data));
    }

    private static PackageFilteringSearchService CreateTarget(
        ISearchService inner,
        IPackageDatabase packages,
        IPackagePolicyEvaluator policy)
    {
        var url = new Mock<IUrlGenerator>();
        return new PackageFilteringSearchService(
            inner,
            packages,
            policy,
            new SearchResponseBuilder(url.Object));
    }

    private static Mock<ISearchService> SearchServiceWithResults(params SearchResult[] results)
    {
        var inner = new Mock<ISearchService>();
        inner
            .Setup(service => service.SearchAsync(
                It.IsAny<SearchRequest>(),
                CancellationToken))
            .ReturnsAsync(new SearchResponse { Data = results });
        return inner;
    }

    private static Mock<ISearchService> SearchServiceWithPagedResults(
        IReadOnlyList<SearchResult> results)
    {
        var inner = new Mock<ISearchService>();
        inner
            .Setup(service => service.SearchAsync(
                It.IsAny<SearchRequest>(),
                CancellationToken))
            .ReturnsAsync((SearchRequest request, CancellationToken _) => new SearchResponse
            {
                Data = results.Skip(request.Skip).Take(request.Take).ToList()
            });
        return inner;
    }

    private static Mock<IPackageDatabase> PackageDatabase(
        params (string PackageId, Package[] Packages)[] packages)
    {
        var packagesById = packages.ToDictionary(
            entry => entry.PackageId,
            entry => (IReadOnlyList<Package>)entry.Packages,
            StringComparer.OrdinalIgnoreCase);
        var database = new Mock<IPackageDatabase>();
        database
            .Setup(service => service.FindAsync(
                It.IsAny<string>(),
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, bool _, CancellationToken _) =>
                packagesById.GetValueOrDefault(id, Array.Empty<Package>()));
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

    private static SearchResult SearchResult(string packageId, params string[] versions)
        => new()
        {
            PackageId = packageId,
            Version = versions.Last(),
            Versions = versions
                .Select(version => new SearchResultVersion { Version = version })
                .ToList()
        };

    private static Package CachedPackage(string packageId, string version)
    {
        var package = LocalPackage(packageId, version);
        package.CachedFrom = "https://packages.example/v3/index.json";
        return package;
    }

    private static Package LocalPackage(string packageId, string version)
    {
        var package = Generator.GetPackage(packageId, version);
        package.Listed = true;
        return package;
    }
}
