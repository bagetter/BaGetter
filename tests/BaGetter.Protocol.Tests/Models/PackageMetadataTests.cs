using System.Text.Json;
using BaGetter.Protocol.Internal;
using BaGetter.Protocol.Models;
using Xunit;

namespace BaGetter.Protocol.Tests.Models;

public class PackageMetadataTests
{
    private readonly JsonSerializerOptions _serializerOptions;

    public PackageMetadataTests()
    {
        _serializerOptions = new JsonSerializerOptions();
        _serializerOptions.Converters.Add(new StringOrStringArrayJsonConverter());
    }

    [Fact]
    public void Tags_EmptyArray_ShouldDeserialize()
    {
        // Arrange
        var stringToDeserialize = """
        {
          "@id": "https://nuget.pkg.github.com/FakeOrga/fakepkg/1.5.1.json",
          "authors": "Someone",
          "copyright": "",
          "dependencyGroups": [ ],
          "description": "Package Description",
          "iconUrl": "",
          "id": "fakepkg",
          "isPrerelease": false,
          "language": "",
          "licenseUrl": "",
          "packageContent": "https://nuget.pkg.github.com/FakeOrga/download/fakepkg/1.5.1/fakepkg.1.5.1.nupkg",
          "projectUrl": "",
          "requireLicenseAcceptance": false,
          "summary": "",
          "tags": [ ],
          "version": "1.5.1"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<PackageMetadata>(stringToDeserialize, _serializerOptions);

        // Assert
        Assert.NotNull(result.Tags);
        Assert.Empty(result.Tags);
    }

    [Fact]
    public void Tags_EmptyString_ShouldDeserialize()
    {
        // Arrange
        var stringToDeserialize = """
        {
          "@id": "https://nuget.pkg.github.com/FakeOrga/fakepkg/1.5.1.json",
          "authors": "Someone",
          "copyright": "",
          "dependencyGroups": [ ],
          "description": "Package Description",
          "iconUrl": "",
          "id": "fakepkg",
          "isPrerelease": false,
          "language": "",
          "licenseUrl": "",
          "packageContent": "https://nuget.pkg.github.com/FakeOrga/download/fakepkg/1.5.1/fakepkg.1.5.1.nupkg",
          "projectUrl": "",
          "requireLicenseAcceptance": false,
          "summary": "",
          "tags": "",
          "version": "1.5.1"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<PackageMetadata>(stringToDeserialize, _serializerOptions);

        // Assert
        Assert.NotNull(result.Tags);
        Assert.Single(result.Tags);
    }

    [Fact]
    public void Tags_SingleEntry_ShouldDeserializeSingleTag()
    {
        // Arrange
        var stringToDeserialize = """
        {
          "@id": "https://nuget.pkg.github.com/FakeOrga/fakepkg/1.5.1.json",
          "authors": "Someone",
          "copyright": "",
          "dependencyGroups": [ ],
          "description": "Package Description",
          "iconUrl": "",
          "id": "fakepkg",
          "isPrerelease": false,
          "language": "",
          "licenseUrl": "",
          "packageContent": "https://nuget.pkg.github.com/FakeOrga/download/fakepkg/1.5.1/fakepkg.1.5.1.nupkg",
          "projectUrl": "",
          "requireLicenseAcceptance": false,
          "summary": "",
          "tags": "tag1",
          "version": "1.5.1"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<PackageMetadata>(stringToDeserialize, _serializerOptions);

        // Assert
        Assert.NotNull(result.Tags);
        Assert.Single(result.Tags);
    }

    [Fact]
    public void Tags_HasMultipleEntries_ShouldDeserializeMultipleTags()
    {
        // Arrange
        var stringToDeserialize = """
        {
          "@id": "https://nuget.pkg.github.com/FakeOrga/fakepkg/1.5.1.json",
          "authors": "Someone",
          "copyright": "",
          "dependencyGroups": [ ],
          "description": "Package Description",
          "iconUrl": "",
          "id": "fakepkg",
          "isPrerelease": false,
          "language": "",
          "licenseUrl": "",
          "packageContent": "https://nuget.pkg.github.com/FakeOrga/download/fakepkg/1.5.1/fakepkg.1.5.1.nupkg",
          "projectUrl": "",
          "requireLicenseAcceptance": false,
          "summary": "",
          "tags": ["tag1", "tag2"],
          "version": "1.5.1"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<PackageMetadata>(stringToDeserialize, _serializerOptions);

        // Assert
        Assert.NotNull(result.Tags);
        Assert.Equal(2, result.Tags.Count);
    }

}
