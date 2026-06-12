using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace BaGetter.Tests;

public class PackageFilteringIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PackageFilteringIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task DisabledFilteringAllowsUpstreamDownload()
    {
        using var upstream = new BaGetterApplication(_output);
        using var downstream = CreateDownstream(upstream.CreateClient(), enabled: false);
        using var package = TestResources.GetResourceStream(TestResources.Package);
        await upstream.AddPackageAsync(package);

        using var response = await downstream.CreateClient().GetAsync(
            "v3/package/TestData/1.2.3/TestData.1.2.3.nupkg");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BlockedDownloadReturnsNotFound()
    {
        using var upstream = new BaGetterApplication(_output);
        using var downstream = CreateDownstream(upstream.CreateClient());
        using var package = TestResources.GetResourceStream(TestResources.Package);
        await upstream.AddPackageAsync(package);

        using var response = await downstream.CreateClient().GetAsync(
            "v3/package/TestData/1.2.3/TestData.1.2.3.nupkg");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BlockedVersionsAreRemovedFromMetadataAndVersionList()
    {
        using var upstream = new BaGetterApplication(_output);
        using var downstream = CreateDownstream(upstream.CreateClient());
        using var package = TestResources.GetResourceStream(TestResources.Package);
        await upstream.AddPackageAsync(package);
        using var client = downstream.CreateClient();

        using var versions = await client.GetAsync("v3/package/TestData/index.json");
        using var metadata = await client.GetAsync("v3/registration/TestData/index.json");

        Assert.Equal(HttpStatusCode.NotFound, versions.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, metadata.StatusCode);
    }

    [Fact]
    public async Task VersionRangeBlocksOnlyMatchingUpstreamVersions()
    {
        using var upstream = new BaGetterApplication(_output);
        using var downstream = CreateDownstream(
            upstream.CreateClient(),
            blockedPackageId: "Example.Range",
            blockedVersions: "[2.0.0,)");
        using var allowedPackage = CreatePackage("Example.Range", "1.0.0");
        using var blockedPackage = CreatePackage("Example.Range", "2.0.0");
        await upstream.AddPackageAsync(allowedPackage);
        await upstream.AddPackageAsync(blockedPackage);
        using var client = downstream.CreateClient();

        using var versions = await client.GetAsync("v3/package/Example.Range/index.json");
        var versionsContent = await versions.Content.ReadAsStringAsync();
        using var metadata = await client.GetAsync("v3/registration/Example.Range/index.json");
        var metadataContent = await metadata.Content.ReadAsStringAsync();
        using var allowedDownload = await client.GetAsync(
            "v3/package/Example.Range/1.0.0/Example.Range.1.0.0.nupkg");
        using var blockedDownload = await client.GetAsync(
            "v3/package/Example.Range/2.0.0/Example.Range.2.0.0.nupkg");

        Assert.Equal(HttpStatusCode.OK, versions.StatusCode);
        Assert.Contains(@"""1.0.0""", versionsContent);
        Assert.DoesNotContain(@"""2.0.0""", versionsContent);
        Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);
        Assert.Contains(@"""version"":""1.0.0""", metadataContent);
        Assert.DoesNotContain(@"""version"":""2.0.0""", metadataContent);
        Assert.Equal(HttpStatusCode.OK, allowedDownload.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, blockedDownload.StatusCode);
    }

    [Fact]
    public async Task CachedUpstreamPackageIsBlockedFromClientSurfaces()
    {
        using var app = CreateDownstream(upstreamClient: null);
        using var package = TestResources.GetResourceStream(TestResources.Package);
        await app.AddCachedPackageAsync(package, "https://api.nuget.org/v3/index.json");
        using var client = app.CreateClient();

        using var download = await client.GetAsync(
            "v3/package/TestData/1.2.3/TestData.1.2.3.nupkg");
        using var search = await client.GetAsync("v3/search");
        var searchContent = await search.Content.ReadAsStringAsync();
        using var versions = await client.GetAsync("v3/package/TestData/index.json");
        using var metadata = await client.GetAsync("v3/registration/TestData/index.json");

        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        Assert.Contains(@"""totalHits"":0", searchContent);
        Assert.Equal(HttpStatusCode.NotFound, versions.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, metadata.StatusCode);
    }

    [Fact]
    public async Task LocalPackageIsNotBlocked()
    {
        using var app = CreateDownstream(upstreamClient: null);
        using var package = TestResources.GetResourceStream(TestResources.Package);
        await app.AddPackageAsync(package);
        using var client = app.CreateClient();

        using var download = await client.GetAsync(
            "v3/package/TestData/1.2.3/TestData.1.2.3.nupkg");
        using var search = await client.GetAsync("v3/search");
        var searchContent = await search.Content.ReadAsStringAsync();
        using var metadata = await client.GetAsync("v3/registration/TestData/index.json");

        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        Assert.Contains(@"""id"":""TestData""", searchContent);
        Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);
    }

    [Fact]
    public async Task SearchPaginationSkipsBlockedPackages()
    {
        using var app = CreateDownstream(
            upstreamClient: null,
            blockedPackageId: "Example.ABlocked");
        using var blockedPackage = CreatePackage("Example.ABlocked");
        using var allowedPackage = CreatePackage("Example.ZAllowed");
        await app.AddCachedPackageAsync(blockedPackage, "https://packages.example/v3/index.json");
        await app.AddPackageAsync(allowedPackage);

        using var response = await app.CreateClient().GetAsync("v3/search?take=1");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(@"""id"":""Example.ZAllowed""", content);
        Assert.DoesNotContain(@"""id"":""Example.ABlocked""", content);
    }

    private BaGetterApplication CreateDownstream(
        HttpClient upstreamClient,
        bool enabled = true,
        string blockedPackageId = "TestData",
        string blockedVersions = "*")
    {
        return new BaGetterApplication(
            _output,
            upstreamClient,
            configuration =>
            {
                configuration["PackageFiltering:Enabled"] = enabled.ToString();
                configuration["PackageFiltering:BlockCachedPackages"] = "true";
                configuration["PackageFiltering:Rules:0:PackageId"] = blockedPackageId;
                configuration["PackageFiltering:Rules:0:Versions"] = blockedVersions;
            });
    }

    private static MemoryStream CreatePackage(string packageId, string version = "1.0.0")
    {
        var builder = new PackageBuilder
        {
            Id = packageId,
            Version = NuGetVersion.Parse(version),
            Description = "Generated test package"
        };
        builder.Authors.Add("Test Author");
        builder.Files.Add(new PhysicalPackageFile
        {
            SourcePath = typeof(PackageFilteringIntegrationTests).Assembly.Location,
            TargetPath = "lib/net9.0/Test.dll"
        });

        var stream = new MemoryStream();
        builder.Save(stream);
        stream.Position = 0;
        return stream;
    }
}
