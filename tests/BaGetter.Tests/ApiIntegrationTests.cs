using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BaGetter.Tests;

public class ApiIntegrationTests : IDisposable
{
    private readonly BaGetterApplication _app;
    private readonly HttpClient _client;

    private readonly Stream _packageStream;
    private readonly Stream _symbolPackageStream;

    private readonly ITestOutputHelper _output;

    public ApiIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _app = new BaGetterApplication(_output);
        _client = _app.CreateClient();

        _packageStream = TestResources.GetResourceStream(TestResources.Package);
        _symbolPackageStream = TestResources.GetResourceStream(TestResources.SymbolPackage);
    }

    [Fact]
    public async Task IndexReturnsOk()
    {
        using var response = await _client.GetAsync("v3/index.json");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TestData.ServiceIndex, content);
    }

    [Fact]
    public async Task SearchReturnsOk()
    {
        await _app.AddPackageAsync(_packageStream);

        using var response = await _client.GetAsync("v3/search");
        var content = await response.Content.ReadAsStreamAsync();
        var json = content.ToPrettifiedJson();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(@"{
  ""@context"": {
    ""@vocab"": ""http://schema.nuget.org/schema#"",
    ""@base"": ""http://localhost/v3/registration/""
  },
  ""totalHits"": 1,
  ""data"": [
    {
      ""id"": ""TestData"",
      ""version"": ""1.2.3"",
      ""description"": ""Test description"",
      ""authors"": [
        ""Test author""
      ],
      ""iconUrl"": """",
      ""licenseUrl"": """",
      ""projectUrl"": """",
      ""registration"": ""http://localhost/v3/registration/testdata/index.json"",
      ""summary"": """",
      ""tags"": [],
      ""title"": """",
      ""totalDownloads"": 0,
      ""versions"": [
        {
          ""@id"": ""http://localhost/v3/registration/testdata/1.2.3.json"",
          ""version"": ""1.2.3"",
          ""downloads"": 0
        }
      ]
    }
  ]
}", json);
    }

    [Fact]
    public async Task SearchReturnsEmpty()
    {
        using var response = await _client.GetAsync("v3/search?q=PackageDoesNotExist");
        var content = await response.Content.ReadAsStreamAsync();
        var json = content.ToPrettifiedJson();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(@"{
  ""@context"": {
    ""@vocab"": ""http://schema.nuget.org/schema#"",
    ""@base"": ""http://localhost/v3/registration/""
  },
  ""totalHits"": 0,
  ""data"": []
}", json);
    }

    [Fact]
    public async Task IndexPageAuthorQueryReturnsOk()
    {
        await _app.AddPackageAsync(_packageStream);

        using var response = await _client.GetAsync("/?q=author:%22Test%20author%22%20TestData");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("TestData", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AutocompleteReturnsOk()
    {
        await _app.AddPackageAsync(_packageStream);

        using var response = await _client.GetAsync("v3/autocomplete");
        var content = await response.Content.ReadAsStreamAsync();
        var json = content.ToPrettifiedJson();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(@"{
  ""@context"": {
    ""@vocab"": ""http://schema.nuget.org/schema#""
  },
  ""totalHits"": 1,
  ""data"": [
    ""TestData""
  ]
}", json);
    }

    [Fact]
    public async Task AutocompleteReturnsEmpty()
    {
        using var response = await _client.GetAsync("v3/autocomplete?q=PackageDoesNotExist");
        var content = await response.Content.ReadAsStreamAsync();
        var json = content.ToPrettifiedJson();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(@"{
  ""@context"": {
    ""@vocab"": ""http://schema.nuget.org/schema#""
  },
  ""totalHits"": 0,
  ""data"": []
}", json);
    }

    [Fact]
    public async Task AutocompleteVersionsReturnsOk()
    {
        await _app.AddPackageAsync(_packageStream);

        using var response = await _client.GetAsync("v3/autocomplete?id=TestData");
        var content = await response.Content.ReadAsStreamAsync();
        var json = content.ToPrettifiedJson();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(@"{
  ""@context"": {
    ""@vocab"": ""http://schema.nuget.org/schema#""
  },
  ""totalHits"": 1,
  ""data"": [
    ""1.2.3""
  ]
}", json);
    }

    [Fact]
    public async Task AutocompleteVersionsReturnsEmpty()
    {
        using var response = await _client.GetAsync("v3/autocomplete?id=PackageDoesNotExist");
        var content = await response.Content.ReadAsStreamAsync();
        var json = content.ToPrettifiedJson();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(@"{
  ""@context"": {
    ""@vocab"": ""http://schema.nuget.org/schema#""
  },
  ""totalHits"": 0,
  ""data"": []
}", json);
    }

    [Fact]
    public async Task VersionListReturnsOk()
    {
        await _app.AddPackageAsync(_packageStream);

        var response = await _client.GetAsync("v3/package/TestData/index.json");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(@"{""versions"":[""1.2.3""]}", content);
    }

    [Fact]
    public async Task VersionListReturnsNotFound()
    {
        using var response = await _client.GetAsync("v3/package/PackageDoesNotExist/index.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PackageDownloadReturnsOk()
    {
        await _app.AddPackageAsync(_packageStream);

        using var response = await _client.GetAsync("v3/package/TestData/1.2.3/TestData.1.2.3.nupkg");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PackageDownloadReturnsNotFound()
    {
        using var response = await _client.GetAsync(
            "v3/package/PackageDoesNotExist/1.0.0/PackageDoesNotExist.1.0.0.nupkg");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NuspecDownloadReturnsOk()
    {
        await _app.AddPackageAsync(_packageStream);

        using var response = await _client.GetAsync(
            "v3/package/TestData/1.2.3/TestData.1.2.3.nuspec");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NuspecDownloadReturnsNotFound()
    {
        using var response = await _client.GetAsync(
            "v3/package/PackageDoesNotExist/1.0.0/PackageDoesNotExist.1.0.0.nuspec");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PackageMetadataReturnsOk()
    {
        // Arrange
        await _app.AddPackageAsync(_packageStream);

        const string expectedResponse = """
            {
              "@id": "http://localhost/v3/registration/testdata/index.json",
              "@type": [
                "catalog:CatalogRoot",
                "PackageRegistration",
                "catalog:Permalink"
              ],
              "count": 1,
              "items": [
                {
                  "@id": "http://localhost/v3/registration/testdata/index.json",
                  "count": 1,
                  "lower": "1.2.3",
                  "upper": "1.2.3",
                  "items": [
                    {
                      "@id": "http://localhost/v3/registration/testdata/1.2.3.json",
                      "packageContent": "http://localhost/v3/package/testdata/1.2.3/testdata.1.2.3.nupkg",
                      "catalogEntry": {
                        "downloads": 0,
                        "hasReadme": false,
                        "packageTypes": [
                          "Dependency"
                        ],
                        "releaseNotes": "",
                        "repositoryUrl": "",
                        "id": "TestData",
                        "version": "1.2.3",
                        "authors": "Test author",
                        "dependencyGroups": [
                          {
                            "targetFramework": "net5.0",
                            "dependencies": []
                          }
                        ],
                        "description": "Test description",
                        "iconUrl": "",
                        "language": "",
                        "licenseUrl": "",
                        "listed": true,
                        "minClientVersion": "",
                        "packageContent": "http://localhost/v3/package/testdata/1.2.3/testdata.1.2.3.nupkg",
                        "projectUrl": "",
                        "published": "2020-01-01T00:00:00Z",
                        "requireLicenseAcceptance": false,
                        "summary": "",
                        "tags": [],
                        "title": ""
                      }
                    }
                  ]
                }
              ],
              "totalDownloads": 0
            }
            """;

        // Act
        using var response = await _client.GetAsync("v3/registration/TestData/index.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStreamAsync();
        var actualResponse = content.ToPrettifiedJson();

        _output.WriteLine($"actual response:{Environment.NewLine}{actualResponse}");
        _output.WriteLine($"expected response:{Environment.NewLine}{expectedResponse}");

        Assert.Equal(expectedResponse, actualResponse);
    }

    [Fact]
    public async Task PackageMetadataReturnsNotModifiedWhenIfNoneMatchMatches()
    {
        await _app.AddPackageAsync(_packageStream);

        using var firstResponse = await _client.GetAsync("v3/registration/TestData/index.json");
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var etag = firstResponse.Headers.ETag?.Tag;
        Assert.False(string.IsNullOrWhiteSpace(etag));

        var firstCacheControl = firstResponse.Headers.CacheControl?.ToString() ?? string.Empty;
        Assert.Contains("public", firstCacheControl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=0", firstCacheControl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must-revalidate", firstCacheControl, StringComparison.OrdinalIgnoreCase);

        using var request = new HttpRequestMessage(HttpMethod.Get, "v3/registration/TestData/index.json");
        request.Headers.TryAddWithoutValidation("If-None-Match", etag);

        using var secondResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotModified, secondResponse.StatusCode);

        var secondCacheControl = secondResponse.Headers.CacheControl?.ToString() ?? string.Empty;
        Assert.Contains("public", secondCacheControl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=0", secondCacheControl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must-revalidate", secondCacheControl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PackageMetadataReturnsNotFound()
    {
        using var response = await _client.GetAsync("v3/registration/PackageDoesNotExist/index.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PackageMetadataLeafReturnsOk()
    {
        await _app.AddPackageAsync(_packageStream);

        using var response = await _client.GetAsync("v3/registration/TestData/1.2.3.json");
        var content = await response.Content.ReadAsStreamAsync();
        var json = content.ToPrettifiedJson();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(@"{
  ""@id"": ""http://localhost/v3/registration/testdata/1.2.3.json"",
  ""@type"": [
    ""Package"",
    ""http://schema.nuget.org/catalog#Permalink""
  ],
  ""listed"": true,
  ""packageContent"": ""http://localhost/v3/package/testdata/1.2.3/testdata.1.2.3.nupkg"",
  ""published"": ""2020-01-01T00:00:00Z"",
  ""registration"": ""http://localhost/v3/registration/testdata/index.json""
}", json);
    }

    [Fact]
    public async Task PackageMetadataLeafReturnsNotFound()
    {
        using var response = await _client.GetAsync("v3/registration/PackageDoesNotExist/1.0.0.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PackageMetadataPageReturnsOk()
    {
        await _app.AddPackageAsync(_packageStream);

        using var response = await _client.GetAsync("v3/registration/TestData/page/1.2.3/1.2.3.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);

        Assert.Equal(1, document.RootElement.GetProperty("count").GetInt32());
        Assert.Equal("1.2.3", document.RootElement.GetProperty("lower").GetString());
        Assert.Equal("1.2.3", document.RootElement.GetProperty("upper").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task PackageMetadataPageReturnsNotFound()
    {
        using var response = await _client.GetAsync("v3/registration/PackageDoesNotExist/page/1.0.0/1.0.0.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PackageMetadataReturnsPagedIndexWhenRegistrationPageSizeIsSmall()
    {
        using var app = new BaGetterApplication(_output, inMemoryConfiguration: config => config["RegistrationPageSize"] = "2");
        using var client = app.CreateClient();

        await using var package100 = TestResources.GetPackageStreamWithVersion("1.0.0");
        await using var package110 = TestResources.GetPackageStreamWithVersion("1.1.0");
        await using var package120 = TestResources.GetPackageStreamWithVersion("1.2.0");

        await app.AddPackageAsync(package100);
        await app.AddPackageAsync(package110);
        await app.AddPackageAsync(package120);

        using var response = await client.GetAsync("v3/registration/TestData/index.json");
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, document.RootElement.GetProperty("count").GetInt32());

        var pages = document.RootElement.GetProperty("items");
        Assert.Equal(2, pages.GetArrayLength());

        Assert.False(pages[0].TryGetProperty("items", out _));
        Assert.False(pages[1].TryGetProperty("items", out _));

        var firstPageUrl = GetRegistrationPageUrl(pages[0]);
        var secondPageUrl = GetRegistrationPageUrl(pages[1]);

        Assert.Contains("/v3/registration/testdata/page/", firstPageUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/v3/registration/testdata/page/", secondPageUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PackageMetadataPageReturnsInlinedItemsForPagedRegistration()
    {
        using var app = new BaGetterApplication(_output, inMemoryConfiguration: config => config["RegistrationPageSize"] = "2");
        using var client = app.CreateClient();

        await using var package100 = TestResources.GetPackageStreamWithVersion("1.0.0");
        await using var package110 = TestResources.GetPackageStreamWithVersion("1.1.0");
        await using var package120 = TestResources.GetPackageStreamWithVersion("1.2.0");

        await app.AddPackageAsync(package100);
        await app.AddPackageAsync(package110);
        await app.AddPackageAsync(package120);

        using var indexResponse = await client.GetAsync("v3/registration/TestData/index.json");
        var indexContent = await indexResponse.Content.ReadAsStringAsync();
        using var indexDocument = JsonDocument.Parse(indexContent);

        var firstPageUrl = GetRegistrationPageUrl(indexDocument.RootElement.GetProperty("items")[0]);
        using var pageResponse = await client.GetAsync(firstPageUrl);
        var pageContent = await pageResponse.Content.ReadAsStringAsync();
        using var pageDocument = JsonDocument.Parse(pageContent);

        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.Equal(2, pageDocument.RootElement.GetProperty("count").GetInt32());
        Assert.Equal(2, pageDocument.RootElement.GetProperty("items").GetArrayLength());
        Assert.False(string.IsNullOrWhiteSpace(pageResponse.Headers.ETag?.Tag));
    }

    private static string GetRegistrationPageUrl(JsonElement pageElement)
    {
        if (pageElement.TryGetProperty("@id", out var idProperty))
        {
            return idProperty.GetString();
        }

        return pageElement.GetProperty("registrationPageUrl").GetString();
    }

    [Fact]
    public async Task PackageDependentsReturnsOk()
    {
        using var response = await _client.GetAsync("v3/dependents?packageId=TestData");

        var content = await response.Content.ReadAsStreamAsync();
        var json = content.ToPrettifiedJson();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(@"{
  ""totalHits"": 0,
  ""data"": []
}", json);
    }

    [Fact]
    public async Task PackageDependentsReturnsBadRequest()
    {
        using var response = await _client.GetAsync("v3/dependents");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PackageDownloadReturnsNotModifiedWhenIfNoneMatchMatches()
    {
        await _app.AddPackageAsync(_packageStream);

        using var firstResponse = await _client.GetAsync("v3/package/TestData/1.2.3/TestData.1.2.3.nupkg");
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var etag = firstResponse.Headers.ETag?.Tag;
        Assert.False(string.IsNullOrWhiteSpace(etag));

        var firstCacheControl = firstResponse.Headers.CacheControl?.ToString() ?? string.Empty;
        Assert.Contains("public", firstCacheControl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=31536000", firstCacheControl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("immutable", firstCacheControl, StringComparison.OrdinalIgnoreCase);

        using var request = new HttpRequestMessage(HttpMethod.Get, "v3/package/TestData/1.2.3/TestData.1.2.3.nupkg");
        request.Headers.TryAddWithoutValidation("If-None-Match", etag);

        using var secondResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotModified, secondResponse.StatusCode);

        var secondCacheControl = secondResponse.Headers.CacheControl?.ToString() ?? string.Empty;
        Assert.Contains("public", secondCacheControl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=31536000", secondCacheControl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("immutable", secondCacheControl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SymbolDownloadReturnsOk()
    {
        await _app.AddPackageAsync(_packageStream);
        await _app.AddSymbolPackageAsync(_symbolPackageStream);

        using var response = await _client.GetAsync(
            "api/download/symbols/testdata.pdb/16F71ED8DD574AA2AD4A22D29E9C981Bffffffff/testdata.pdb");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("api/download/symbols/testdata.pdb/16F71ED8DD574AA2AD4A22D29E9C981B1/testdata.pdb")]
    [InlineData("api/download/symbols/testdata.pdb/16F71ED8DD574AA2AD4A22D29E9C981B/testdata.pdb")]
    [InlineData("api/download/symbols/testprefix/testdata.pdb/16F71ED8DD574AA2AD4A22D29E9C981Bffffffff/testdata.pdb")]
    public async Task MalformedSymbolDownloadReturnsOk(string uri)
    {
        await _app.AddPackageAsync(_packageStream);
        await _app.AddSymbolPackageAsync(_symbolPackageStream);

        using var response = await _client.GetAsync(uri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SymbolDownloadReturnsNotFound()
    {
        using var response = await _client.GetAsync(
            "api/download/symbols/doesnotexist.pdb/16F71ED8DD574AA2AD4A22D29E9C981Bffffffff/doesnotexist.pdb");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RateLimitingReturnsTooManyRequestsWhenEnabled()
    {
        using var app = new BaGetterApplication(_output, inMemoryConfiguration: config =>
        {
            config["RequestRateLimit:Enabled"] = "true";
            config["RequestRateLimit:PermitLimit"] = "1";
            config["RequestRateLimit:WindowSeconds"] = "60";
            config["RequestRateLimit:QueueLimit"] = "0";
        });
        using var client = app.CreateClient();

        using var firstResponse = await client.GetAsync("v3/index.json");
        using var secondResponse = await client.GetAsync("v3/index.json");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
    }

    [Fact]
    public async Task SecurityHeadersAreIncludedByDefault()
    {
        using var response = await _client.GetAsync("v3/index.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
    }

    [Fact]
    public async Task CorsCanRestrictOrigins()
    {
        using var app = new BaGetterApplication(_output, inMemoryConfiguration: config =>
        {
            config["Cors:AllowAnyOrigin"] = "false";
            config["Cors:AllowedOrigins:0"] = "https://allowed.example";
            config["Cors:AllowAnyMethod"] = "true";
            config["Cors:AllowAnyHeader"] = "true";
        });
        using var client = app.CreateClient();

        using var allowedRequest = new HttpRequestMessage(HttpMethod.Get, "v3/index.json");
        allowedRequest.Headers.Add("Origin", "https://allowed.example");

        using var allowedResponse = await client.SendAsync(allowedRequest);
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        Assert.Equal("https://allowed.example", string.Join(",", allowedResponse.Headers.GetValues("Access-Control-Allow-Origin")));

        using var blockedRequest = new HttpRequestMessage(HttpMethod.Get, "v3/index.json");
        blockedRequest.Headers.Add("Origin", "https://blocked.example");

        using var blockedResponse = await client.SendAsync(blockedRequest);
        Assert.Equal(HttpStatusCode.OK, blockedResponse.StatusCode);
        Assert.False(blockedResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    public void Dispose()
    {
        _app.Dispose();
        _client.Dispose();
    }
}
