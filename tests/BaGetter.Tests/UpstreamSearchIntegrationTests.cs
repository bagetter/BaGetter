using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BaGetter.Tests;

public class UpstreamSearchIntegrationTests : IDisposable
{
    private readonly BaGetterApplication _upstream;
    private readonly BaGetterApplication _downstream;
    private readonly HttpClient _downstreamClient;
    private readonly Stream _packageStream;

    public UpstreamSearchIntegrationTests(ITestOutputHelper output)
    {
        _upstream = new BaGetterApplication(output);
        _downstream = new BaGetterApplication(
            output,
            _upstream.CreateClient(),
            configuration => configuration["Search:IncludeUpstream"] = "true");
        _downstreamClient = _downstream.CreateClient();
        _packageStream = TestResources.GetResourceStream(TestResources.Package);
    }

    [Fact]
    public async Task SearchAndAutocompleteIncludeUpstreamPackages()
    {
        await _upstream.AddPackageAsync(_packageStream);

        using var searchResponse = await _downstreamClient.GetAsync("v3/search?q=TestData");
        var searchContent = await searchResponse.Content.ReadAsStringAsync();
        using var autocompleteResponse = await _downstreamClient.GetAsync("v3/autocomplete?q=Test");
        var autocompleteContent = await autocompleteResponse.Content.ReadAsStringAsync();
        using var versionsResponse = await _downstreamClient.GetAsync("v3/autocomplete?id=TestData");
        var versionsContent = await versionsResponse.Content.ReadAsStringAsync();
        using var browseResponse = await _downstreamClient.GetAsync("/?q=TestData");
        var browseContent = await browseResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        Assert.Contains(@"""id"":""TestData""", searchContent);
        Assert.Equal(HttpStatusCode.OK, autocompleteResponse.StatusCode);
        Assert.Contains(@"""TestData""", autocompleteContent);
        Assert.Equal(HttpStatusCode.OK, versionsResponse.StatusCode);
        Assert.Contains(@"""1.2.3""", versionsContent);
        Assert.Equal(HttpStatusCode.OK, browseResponse.StatusCode);
        Assert.Contains("TestData", browseContent);
    }

    public void Dispose()
    {
        _packageStream.Dispose();
        _upstream.Dispose();
        _downstream.Dispose();
    }
}
