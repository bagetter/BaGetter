using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BaGetter.Tests.Support;
using Xunit;
using Xunit.Abstractions;

namespace BaGetter.Tests;

/// <summary>
/// Verifies end-to-end behavior of a named (non-default) feed:
/// packages pushed via the slug-prefixed URL are searchable and downloadable
/// through the same prefix, and the service-index for that feed lists
/// feed-prefixed URLs.
/// </summary>
public class NamedFeedTests : IDisposable
{
    private const string FeedSlug = "my-feed";

    private readonly BaGetterApplication _app;
    private readonly HttpClient _client;
    private readonly Stream _packageStream;

    public NamedFeedTests(ITestOutputHelper output)
    {
        _app = new BaGetterApplication(output);
        _client = _app.CreateClient();
        _packageStream = TestResources.GetResourceStream(TestResources.Package);
    }

    [Fact]
    public async Task ServiceIndexReturnsOkForNamedFeed()
    {
        await _app.CreateFeedAsync(FeedSlug);

        using var response = await _client.GetAsync($"feeds/{FeedSlug}/v3/index.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ServiceIndexUrlsArePrefixedWithFeedSlug()
    {
        await _app.CreateFeedAsync(FeedSlug);

        using var response = await _client.GetAsync($"feeds/{FeedSlug}/v3/index.json");
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var resources = doc.RootElement.GetProperty("resources");

        foreach (var resource in resources.EnumerateArray())
        {
            var id = resource.GetProperty("@id").GetString();
            Assert.Contains($"/feeds/{FeedSlug}/", id, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task UnknownFeedSlugReturnsNotFound()
    {
        using var response = await _client.GetAsync("feeds/does-not-exist/v3/index.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FeedPrefixedStaticAssetsReturnOk()
    {
        await _app.CreateFeedAsync(FeedSlug);

        using var response = await _client.GetAsync(
            $"feeds/{FeedSlug}/_content/BaGetter.Web/images/default-package-icon-256x256.png");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SearchReturnsOkForNamedFeed()
    {
        await _app.CreateFeedAsync(FeedSlug);
        await _app.AddPackageToFeedAsync(_packageStream, FeedSlug);

        using var response = await _client.GetAsync($"feeds/{FeedSlug}/v3/search");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SearchReturnsPackagePushedToNamedFeed()
    {
        await _app.CreateFeedAsync(FeedSlug);
        await _app.AddPackageToFeedAsync(_packageStream, FeedSlug);

        using var response = await _client.GetAsync($"feeds/{FeedSlug}/v3/search");
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("totalHits").GetInt32());

        var first = doc.RootElement.GetProperty("data")[0];
        Assert.Equal("TestData", first.GetProperty("id").GetString());
    }

    [Fact]
    public async Task PackageVersionsReturnsOkForNamedFeed()
    {
        await _app.CreateFeedAsync(FeedSlug);
        await _app.AddPackageToFeedAsync(_packageStream, FeedSlug);

        using var response = await _client.GetAsync($"feeds/{FeedSlug}/v3/package/TestData/index.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PackageDownloadReturnsOkForNamedFeed()
    {
        await _app.CreateFeedAsync(FeedSlug);
        await _app.AddPackageToFeedAsync(_packageStream, FeedSlug);

        using var response = await _client.GetAsync(
            $"feeds/{FeedSlug}/v3/package/TestData/1.2.3/TestData.1.2.3.nupkg");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationIndexReturnsOkForNamedFeed()
    {
        await _app.CreateFeedAsync(FeedSlug);
        await _app.AddPackageToFeedAsync(_packageStream, FeedSlug);

        using var response = await _client.GetAsync(
            $"feeds/{FeedSlug}/v3/registration/TestData/index.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _packageStream?.Dispose();
        _client?.Dispose();
        _app?.Dispose();
    }
}
