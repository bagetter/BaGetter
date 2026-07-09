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
/// Verifies that feeds are isolated: the same package id+version can exist
/// independently in two feeds, and search in one feed does not surface
/// packages from another feed.
/// </summary>
public class FeedIsolationTests : IDisposable
{
    private const string FeedA = "feed-a";
    private const string FeedB = "feed-b";

    private readonly BaGetterApplication _app;
    private readonly HttpClient _client;
    private readonly Stream _packageStream;

    public FeedIsolationTests(ITestOutputHelper output)
    {
        _app = new BaGetterApplication(output);
        _client = _app.CreateClient();
        _packageStream = TestResources.GetResourceStream(TestResources.Package);
    }

    [Fact]
    public async Task SamePackageCanBeIndexedInTwoFeeds()
    {
        await _app.CreateFeedAsync(FeedA);
        await _app.CreateFeedAsync(FeedB);

        // Push the same package to both feeds — both must succeed
        await _app.AddPackageToFeedAsync(_packageStream, FeedA);

        // Re-open stream since it was consumed
        using var stream2 = TestResources.GetResourceStream(TestResources.Package);
        await _app.AddPackageToFeedAsync(stream2, FeedB);

        // Verify both feeds list the package
        using var responseA = await _client.GetAsync($"feeds/{FeedA}/v3/package/TestData/index.json");
        using var responseB = await _client.GetAsync($"feeds/{FeedB}/v3/package/TestData/index.json");

        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
    }

    [Fact]
    public async Task SearchInFeedADoesNotReturnPackagesFromFeedB()
    {
        await _app.CreateFeedAsync(FeedA);
        await _app.CreateFeedAsync(FeedB);

        // Add package only to feed-b
        await _app.AddPackageToFeedAsync(_packageStream, FeedB);

        // Search in feed-a — should be empty
        using var response = await _client.GetAsync($"feeds/{FeedA}/v3/search");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("totalHits").GetInt32());
    }

    [Fact]
    public async Task PackageVersionsInFeedADoesNotReturnPackagesFromFeedB()
    {
        await _app.CreateFeedAsync(FeedA);
        await _app.CreateFeedAsync(FeedB);

        // Add package only to feed-b
        await _app.AddPackageToFeedAsync(_packageStream, FeedB);

        // Version list in feed-a — should be 404
        using var response = await _client.GetAsync($"feeds/{FeedA}/v3/package/TestData/index.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PackageDownloadFromFeedAFailsWhenOnlyInFeedB()
    {
        await _app.CreateFeedAsync(FeedA);
        await _app.CreateFeedAsync(FeedB);

        // Add package only to feed-b
        await _app.AddPackageToFeedAsync(_packageStream, FeedB);

        // Download from feed-a — should be 404
        using var response = await _client.GetAsync(
            $"feeds/{FeedA}/v3/package/TestData/1.2.3/TestData.1.2.3.nupkg");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DefaultFeedIsIsolatedFromNamedFeeds()
    {
        await _app.CreateFeedAsync(FeedA);

        // Add package only to the named feed
        await _app.AddPackageToFeedAsync(_packageStream, FeedA);

        // Default feed search — should be empty
        using var response = await _client.GetAsync("v3/search");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("totalHits").GetInt32());
    }

    public void Dispose()
    {
        _packageStream?.Dispose();
        _client?.Dispose();
        _app?.Dispose();
    }
}
