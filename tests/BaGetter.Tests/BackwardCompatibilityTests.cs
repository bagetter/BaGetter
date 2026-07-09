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
/// Verifies that the default feed continues to respond on legacy root-level URLs
/// (no feed slug prefix) after the multi-feed upgrade. NuGet clients that stored
/// the old service-index URL must not be broken.
/// </summary>
public class BackwardCompatibilityTests : IDisposable
{
    private readonly BaGetterApplication _app;
    private readonly HttpClient _client;
    private readonly Stream _packageStream;

    public BackwardCompatibilityTests(ITestOutputHelper output)
    {
        _app = new BaGetterApplication(output);
        _client = _app.CreateClient();
        _packageStream = TestResources.GetResourceStream(TestResources.Package);
    }

    [Fact]
    public async Task ServiceIndexRespondsOnLegacyRootUrl()
    {
        using var response = await _client.GetAsync("v3/index.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ServiceIndexUrlsAreNotFeedPrefixed()
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
    public async Task SearchRespondsOnLegacyRootUrl()
    {
        await _app.AddPackageAsync(_packageStream);

        using var response = await _client.GetAsync("v3/search");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AutocompleteRespondsOnLegacyRootUrl()
    {
        using var response = await _client.GetAsync("v3/autocomplete");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationIndexRespondsOnLegacyRootUrl()
    {
        await _app.AddPackageAsync(_packageStream);

        using var response = await _client.GetAsync("v3/registration/TestData/index.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PackageVersionsRespondsOnLegacyRootUrl()
    {
        await _app.AddPackageAsync(_packageStream);

        using var response = await _client.GetAsync("v3/package/TestData/index.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PackageDownloadRespondsOnLegacyRootUrl()
    {
        await _app.AddPackageAsync(_packageStream);

        using var response = await _client.GetAsync("v3/package/TestData/1.2.3/TestData.1.2.3.nupkg");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PackagePublishRespondsOnLegacyRootUrl()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(_packageStream), "package", "package.nupkg");

        using var response = await _client.PutAsync("api/v2/package", content);

        // Auth is anonymous in tests, so we expect Created (success) not auth failure.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationUrlsInServiceIndexPointToLegacyRootPaths()
    {
        using var response = await _client.GetAsync("v3/index.json");
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var resources = doc.RootElement.GetProperty("resources");

        var foundRegistrations = false;
        foreach (var resource in resources.EnumerateArray())
        {
            var type = resource.GetProperty("@type").GetString();
            var id = resource.GetProperty("@id").GetString();

            if (type != null && type.StartsWith("RegistrationsBaseUrl", StringComparison.Ordinal))
            {
                foundRegistrations = true;
                Assert.Contains("/v3/registration/", id, StringComparison.Ordinal);
            }
        }

        Assert.True(foundRegistrations, "Service index must contain at least one RegistrationsBaseUrl entry.");
    }

    public void Dispose()
    {
        _packageStream?.Dispose();
        _client?.Dispose();
        _app?.Dispose();
    }
}
