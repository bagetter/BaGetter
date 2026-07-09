using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using BaGetter.Core.Search;
using BaGetter.Protocol.Models;
using BaGetter.Web.Pages;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BaGetter.Web.Tests.Pages;

public class IndexModelFacts
{
    private readonly IndexModel _target;

    private SearchRequest _capturedRequest;
    private readonly SearchResponse _response = new SearchResponse();
    private readonly CancellationToken _cancellation = CancellationToken.None;

    public IndexModelFacts()
    {
        var search = new Mock<ISearchService>();
        search
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), _cancellation))
            .Callback((SearchRequest r, CancellationToken _) => _capturedRequest = r)
            .ReturnsAsync(_response);

        var feedContext = new Mock<IFeedContext>();
        feedContext.Setup(f => f.CurrentFeed).Returns(new Feed
        {
            Id = Guid.Empty,
            Slug = Feed.DefaultSlug,
            Name = "Default",
        });

        var feedService = new Mock<IFeedService>();
        var permissions = new Mock<IPermissionService>();

        var authOptions = new Mock<IOptionsSnapshot<NugetAuthenticationOptions>>();
        authOptions.Setup(o => o.Value).Returns(new NugetAuthenticationOptions());

        _target = new IndexModel(
            search.Object,
            feedContext.Object,
            feedService.Object,
            permissions.Object,
            authOptions.Object);
    }

    [Fact]
    public async Task DefaultSearch()
    {
        await _target.OnGetAsync(_cancellation);

        Assert.Equal(0, _capturedRequest.Skip);
        Assert.Equal(20, _capturedRequest.Take);
        Assert.True(_capturedRequest.IncludePrerelease);
        Assert.True(_capturedRequest.IncludeSemVer2);
        Assert.Null(_capturedRequest.PackageType);
        Assert.Null(_capturedRequest.Framework);
        Assert.Null(_capturedRequest.Tag);
        Assert.True(_capturedRequest.IncludeFacets);
        Assert.False(_capturedRequest.IncludeUnlisted);
        Assert.Null(_capturedRequest.Query);
    }

    [Fact]
    public async Task AcceptsParameters()
    {
        _target.Prerelease = false;
        _target.PageIndex = 5;
        _target.PackageType = "foo";
        _target.Framework = "bar";
        _target.Tag = "baz";
        _target.Query = "Hello world";

        await _target.OnGetAsync(_cancellation);

        Assert.Equal(80, _capturedRequest.Skip);
        Assert.Equal(20, _capturedRequest.Take);
        Assert.False(_capturedRequest.IncludePrerelease);
        Assert.True(_capturedRequest.IncludeSemVer2);
        Assert.Equal("foo", _capturedRequest.PackageType);
        Assert.Equal("bar", _capturedRequest.Framework);
        Assert.Equal("baz", _capturedRequest.Tag);
        Assert.Equal("Hello world", _capturedRequest.Query);
    }
}
