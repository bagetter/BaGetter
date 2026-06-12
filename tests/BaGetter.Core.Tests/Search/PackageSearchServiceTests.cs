using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Search;

public class PackageSearchServiceTests
{
    [Fact]
    public async Task SearchDisabledUsesLocalServiceOnly()
    {
        var request = new SearchRequest();
        var expected = new SearchResponse();
        var local = new Mock<ISearchService>();
        var upstream = new Mock<IUpstreamClient>();
        local
            .Setup(service => service.SearchAsync(request, default))
            .ReturnsAsync(expected);
        var target = CreateTarget(local, upstream, includeUpstream: false);

        var result = await target.SearchAsync(request, default);

        Assert.Same(expected, result);
        upstream.Verify(
            client => client.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchIncludesUpstreamAndRewritesRegistrationUrls()
    {
        var request = new SearchRequest
        {
            Query = "Example",
            Take = 20,
            IncludeSemVer2 = true
        };
        var local = new Mock<ISearchService>();
        var upstream = new Mock<IUpstreamClient>();
        local
            .Setup(service => service.SearchAsync(It.IsAny<SearchRequest>(), default))
            .ReturnsAsync(EmptySearchResponse());
        upstream
            .Setup(client => client.SearchAsync("Example", 0, 20, false, default))
            .ReturnsAsync(
            [
                new SearchResult
                {
                    PackageId = "Example.Search",
                    Version = "13.0.3",
                    Versions =
                    [
                        new SearchResultVersion { Version = "13.0.3" }
                    ]
                }
            ]);
        var url = new Mock<IUrlGenerator>();
        url.Setup(generator => generator.GetPackageMetadataResourceUrl())
            .Returns("http://localhost/v3/registration/");
        url.Setup(generator => generator.GetRegistrationIndexUrl("Example.Search"))
            .Returns("http://localhost/v3/registration/example.search/index.json");
        url.Setup(generator => generator.GetRegistrationLeafUrl(
                "Example.Search",
                NuGetVersion.Parse("13.0.3")))
            .Returns("http://localhost/v3/registration/example.search/13.0.3.json");
        var target = CreateTarget(local, upstream, url, includeUpstream: true);

        var response = await target.SearchAsync(request, default);

        var package = Assert.Single(response.Data);
        Assert.Equal("Example.Search", package.PackageId);
        Assert.Equal(
            "http://localhost/v3/registration/example.search/index.json",
            package.RegistrationIndexUrl);
        Assert.Equal(
            "http://localhost/v3/registration/example.search/13.0.3.json",
            Assert.Single(package.Versions).RegistrationLeafUrl);
    }

    [Fact]
    public async Task AutocompleteIncludesUpstreamPackageIds()
    {
        var request = new AutocompleteRequest { Query = "Example", Take = 20 };
        var local = new Mock<ISearchService>();
        var upstream = new Mock<IUpstreamClient>();
        local
            .Setup(service => service.AutocompleteAsync(It.IsAny<AutocompleteRequest>(), default))
            .ReturnsAsync(new AutocompleteResponse { Data = [] });
        upstream
            .Setup(client => client.AutocompleteAsync("Example", 0, 20, false, default))
            .ReturnsAsync(["Example.Search"]);
        var target = CreateTarget(local, upstream, includeUpstream: true);

        var response = await target.AutocompleteAsync(request, default);

        Assert.Equal(["Example.Search"], response.Data);
    }

    [Fact]
    public async Task VersionListIncludesUpstreamVersions()
    {
        var request = new VersionsRequest
        {
            PackageId = "Example.Search",
            IncludeSemVer2 = true
        };
        var local = new Mock<ISearchService>();
        var upstream = new Mock<IUpstreamClient>();
        local
            .Setup(service => service.ListPackageVersionsAsync(request, default))
            .ReturnsAsync(new AutocompleteResponse { Data = ["12.0.3"] });
        upstream
            .Setup(client => client.ListPackageVersionsAsync("Example.Search", default))
            .ReturnsAsync([NuGetVersion.Parse("13.0.3")]);
        var target = CreateTarget(local, upstream, includeUpstream: true);

        var response = await target.ListPackageVersionsAsync(request, default);

        Assert.Equal(["12.0.3", "13.0.3"], response.Data);
    }

    [Fact]
    public async Task SearchRewritesUpstreamIconUrlWhenFullProxyEnabled()
    {
        var request = new SearchRequest { Query = "Example", Take = 20, IncludeSemVer2 = true };
        var local = new Mock<ISearchService>();
        var upstream = new Mock<IUpstreamClient>();
        local
            .Setup(service => service.SearchAsync(It.IsAny<SearchRequest>(), default))
            .ReturnsAsync(EmptySearchResponse());
        upstream
            .Setup(client => client.SearchAsync("Example", 0, 20, false, default))
            .ReturnsAsync(
            [
                new SearchResult
                {
                    PackageId = "Example.Search",
                    Version = "13.0.3",
                    IconUrl = "https://api.example.com/example.search/13.0.3/icon",
                    Versions = [new SearchResultVersion { Version = "13.0.3" }]
                }
            ]);
        var url = new Mock<IUrlGenerator>();
        url.Setup(generator => generator.GetPackageMetadataResourceUrl())
            .Returns("http://localhost/v3/registration/");
        var rewriter = new Mock<INuGetUrlRewriter>();
        rewriter.Setup(r => r.RewriteAssetUrl(It.IsAny<string>())).Returns((string s) => s);
        rewriter
            .Setup(r => r.RewriteAssetUrl("https://api.example.com/example.search/13.0.3/icon"))
            .Returns("http://localhost/v3/proxy/asset?url=icon");
        var target = CreateTarget(
            local,
            upstream,
            url,
            includeUpstream: true,
            rewriter: rewriter.Object,
            fullProxy: new FullProxyOptions { Enabled = true });

        var response = await target.SearchAsync(request, default);

        var package = Assert.Single(response.Data);
        Assert.Equal("http://localhost/v3/proxy/asset?url=icon", package.IconUrl);
    }

    [Fact]
    public async Task SearchRewritesLocalIconUrlWhenFullProxyEnabled()
    {
        var request = new SearchRequest { Query = "Example", Take = 20, IncludeSemVer2 = true };
        var local = new Mock<ISearchService>();
        var upstream = new Mock<IUpstreamClient>();
        local
            .Setup(service => service.SearchAsync(It.IsAny<SearchRequest>(), default))
            .ReturnsAsync(new SearchResponse
            {
                Data =
                [
                    new SearchResult
                    {
                        PackageId = "Example.Search",
                        Version = "13.0.3",
                        IconUrl = "https://api.example.com/example.search/13.0.3/icon",
                        Versions = [new SearchResultVersion { Version = "13.0.3" }]
                    }
                ]
            });
        upstream
            .Setup(client => client.SearchAsync("Example", 0, 20, false, default))
            .ReturnsAsync([]);
        upstream
            .Setup(client => client.SearchAsync("Example.Search", 0, 10, true, default))
            .ReturnsAsync([]);
        var url = new Mock<IUrlGenerator>();
        url.Setup(generator => generator.GetPackageMetadataResourceUrl())
            .Returns("http://localhost/v3/registration/");
        var rewriter = new Mock<INuGetUrlRewriter>();
        rewriter.Setup(r => r.RewriteAssetUrl(It.IsAny<string>())).Returns((string s) => s);
        rewriter
            .Setup(r => r.RewriteAssetUrl("https://api.example.com/example.search/13.0.3/icon"))
            .Returns("http://localhost/v3/proxy/asset?url=icon");
        var target = CreateTarget(
            local,
            upstream,
            url,
            includeUpstream: false,
            rewriter: rewriter.Object,
            fullProxy: new FullProxyOptions { Enabled = true });

        var response = await target.SearchAsync(request, default);

        var package = Assert.Single(response.Data);
        Assert.Equal("http://localhost/v3/proxy/asset?url=icon", package.IconUrl);
    }

    [Fact]
    public async Task SearchCachesUpstreamResultsWhenFullProxyCacheEnabled()
    {
        var request = new SearchRequest { Query = "Example", Take = 20 };
        var local = new Mock<ISearchService>();
        var upstream = new Mock<IUpstreamClient>();
        local
            .Setup(service => service.SearchAsync(It.IsAny<SearchRequest>(), default))
            .ReturnsAsync(EmptySearchResponse());
        upstream
            .Setup(client => client.SearchAsync("Example", 0, 20, false, default))
            .ReturnsAsync(
            [
                new SearchResult
                {
                    PackageId = "Example.Search",
                    Version = "1.0.0",
                    Versions = [new SearchResultVersion { Version = "1.0.0" }]
                }
            ]);
        var target = CreateTarget(
            local,
            upstream,
            new Mock<IUrlGenerator>(),
            includeUpstream: true,
            fullProxy: new FullProxyOptions { Enabled = true, CacheMinutes = 30 });

        await target.SearchAsync(request, default);
        await target.SearchAsync(request, default);

        upstream.Verify(
            client => client.SearchAsync("Example", 0, 20, false, default),
            Times.Once);
    }

    [Fact]
    public async Task SearchEnrichesDisplayedLocalOnlyDownloadCounts()
    {
        var request = new SearchRequest { Query = "", Take = 20 };
        var local = new Mock<ISearchService>();
        var upstream = new Mock<IUpstreamClient>();
        local
            .Setup(service => service.SearchAsync(It.IsAny<SearchRequest>(), default))
            .ReturnsAsync(new SearchResponse
            {
                Data =
                [
                    new SearchResult
                    {
                        PackageId = "Dapper",
                        Version = "2.1.66",
                        TotalDownloads = 2,
                        Versions = [new SearchResultVersion { Version = "2.1.66" }]
                    }
                ]
            });
        upstream
            .Setup(client => client.SearchAsync("", 0, 20, false, default))
            .ReturnsAsync([]);
        upstream
            .Setup(client => client.SearchAsync("Dapper", 0, 10, true, default))
            .ReturnsAsync(
            [
                new SearchResult
                {
                    PackageId = "Dapper",
                    TotalDownloads = 689_655_536
                }
            ]);
        var target = CreateTarget(local, upstream, includeUpstream: true);

        var response = await target.SearchAsync(request, default);

        var package = Assert.Single(response.Data);
        Assert.Equal(689_655_538, package.TotalDownloads);
    }

    [Fact]
    public async Task SearchAddsLocalAndUpstreamDownloadCounts()
    {
        var request = new SearchRequest { Query = "Example", Take = 20, IncludeSemVer2 = true };
        var local = new Mock<ISearchService>();
        var upstream = new Mock<IUpstreamClient>();
        local
            .Setup(service => service.SearchAsync(It.IsAny<SearchRequest>(), default))
            .ReturnsAsync(new SearchResponse
            {
                Data =
                [
                    new SearchResult
                    {
                        PackageId = "Example.Search",
                        Version = "1.0.0",
                        TotalDownloads = 2,
                        Versions = [new SearchResultVersion { Version = "1.0.0" }]
                    }
                ]
            });
        upstream
            .Setup(client => client.SearchAsync("Example", 0, 20, false, default))
            .ReturnsAsync(
            [
                new SearchResult
                {
                    PackageId = "Example.Search",
                    Version = "1.0.0",
                    TotalDownloads = 100,
                    Versions = [new SearchResultVersion { Version = "1.0.0" }]
                }
            ]);
        var target = CreateTarget(local, upstream, includeUpstream: true);

        var response = await target.SearchAsync(request, default);

        var package = Assert.Single(response.Data);
        Assert.Equal(102, package.TotalDownloads);
    }

    private static PackageSearchService CreateTarget(
        Mock<ISearchService> local,
        Mock<IUpstreamClient> upstream,
        bool includeUpstream)
    {
        return CreateTarget(local, upstream, new Mock<IUrlGenerator>(), includeUpstream);
    }

    private static PackageSearchService CreateTarget(
        Mock<ISearchService> local,
        Mock<IUpstreamClient> upstream,
        Mock<IUrlGenerator> url,
        bool includeUpstream,
        INuGetUrlRewriter rewriter = null,
        FullProxyOptions fullProxy = null)
    {
        if (rewriter is null)
        {
            var passthrough = new Mock<INuGetUrlRewriter>();
            passthrough.Setup(r => r.RewriteAssetUrl(It.IsAny<string>())).Returns((string s) => s);
            rewriter = passthrough.Object;
        }

        var hostProvider = new Mock<IUpstreamHostProvider>();
        hostProvider
            .Setup(h => h.EnsureResolvedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new PackageSearchService(
            local.Object,
            upstream.Object,
            url.Object,
            rewriter,
            hostProvider.Object,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new SearchOptions { IncludeUpstream = includeUpstream }),
            Options.Create(fullProxy ?? new FullProxyOptions()));
    }

    private static SearchResponse EmptySearchResponse()
    {
        return new SearchResponse { Data = new List<SearchResult>() };
    }
}
