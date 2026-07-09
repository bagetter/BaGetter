using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BaGetter.Tests.Support;
using Xunit;
using Xunit.Abstractions;

namespace BaGetter.Tests;

/// <summary>
/// Verifies that the service-index URLs generated for each feed are correctly
/// scoped to that feed's path-base:
///   • Root feed (/v3/index.json) → all resource URLs are root-relative (no /feeds/ prefix)
///   • Named feed (/feeds/{slug}/v3/index.json) → all resource URLs contain /feeds/{slug}/
/// </summary>
public class ServiceIndexUrlTests : IDisposable
{
    private const string FeedSlug = "internal";

    private readonly BaGetterApplication _app;
    private readonly HttpClient _client;

    public ServiceIndexUrlTests(ITestOutputHelper output)
    {
        _app = new BaGetterApplication(output);
        _client = _app.CreateClient();
    }

    [Fact]
    public async Task RootServiceIndex_AllResourceUrls_AreRootRelative()
    {
        using var response = await _client.GetAsync("v3/index.json");
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var resources = doc.RootElement.GetProperty("resources");

        foreach (var resource in resources.EnumerateArray())
        {
            var id = resource.GetProperty("@id").GetString();
            Assert.DoesNotContain("/feeds/", id, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task NamedFeedServiceIndex_AllResourceUrls_ContainFeedSlugPrefix()
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

    public void Dispose()
    {
        _client?.Dispose();
        _app?.Dispose();
    }
}
