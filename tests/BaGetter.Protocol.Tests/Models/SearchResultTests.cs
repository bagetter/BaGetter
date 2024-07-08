using System.Text.Json;
using BaGetter.Protocol.Models;
using Xunit;

namespace BaGetter.Protocol.Tests.Models;

public class SearchResultTests
{
    private readonly JsonSerializerOptions _serializerOptions;

    public SearchResultTests()
    {
        _serializerOptions = new JsonSerializerOptions();
    }

    #region Tags

    [Fact]
    public void Tags_EmptyArray_ShouldDeserialize()
    {
        // Arrange
        var stringToDeserialize = """
        {
            "id": "https://nuget.pkg.github.com/FakeOrga/fakepkg/1.5.1.json",
            "version": "1.5.1",
            "description": "Package Description",
            "authors": [ "Someone" ],
            "iconUrl": "",
            "licenseUrl": "",
            "packageTypes": [
                {
                    "name": "myPackageType"
                }
            ],
            "projectUrl": "",
            "registration": "",
            "summary": "",
            "tags": [ ],
            "title": "",
            "totalDownloads": 0,
            "versions": [
                {
                    "@id": "",
                    "version": "",
                    "downloads": 0
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SearchResult>(stringToDeserialize, _serializerOptions);

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
            "id": "https://nuget.pkg.github.com/FakeOrga/fakepkg/1.5.1.json",
            "version": "1.5.1",
            "description": "Package Description",
            "authors": [ "Someone" ],
            "iconUrl": "",
            "licenseUrl": "",
            "packageTypes": [
                {
                    "name": "myPackageType"
                }
            ],
            "projectUrl": "",
            "registration": "",
            "summary": "",
            "tags": "",
            "title": "",
            "totalDownloads": 0,
            "versions": [
                {
                    "@id": "",
                    "version": "",
                    "downloads": 0
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SearchResult>(stringToDeserialize, _serializerOptions);

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
            "id": "https://nuget.pkg.github.com/FakeOrga/fakepkg/1.5.1.json",
            "version": "1.5.1",
            "description": "Package Description",
            "authors": [ "Someone" ],
            "iconUrl": "",
            "licenseUrl": "",
            "packageTypes": [
                {
                    "name": "myPackageType"
                }
            ],
            "projectUrl": "",
            "registration": "",
            "summary": "",
            "tags": "tag1",
            "title": "",
            "totalDownloads": 0,
            "versions": [
                {
                    "@id": "",
                    "version": "",
                    "downloads": 0
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SearchResult>(stringToDeserialize, _serializerOptions);

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
            "id": "https://nuget.pkg.github.com/FakeOrga/fakepkg/1.5.1.json",
            "version": "1.5.1",
            "description": "Package Description",
            "authors": [ "Someone" ],
            "iconUrl": "",
            "licenseUrl": "",
            "packageTypes": [
                {
                    "name": "myPackageType"
                }
            ],
            "projectUrl": "",
            "registration": "",
            "summary": "",
            "tags": [ "tag1", "tag2"],
            "title": "",
            "totalDownloads": 0,
            "versions": [
                {
                    "@id": "",
                    "version": "",
                    "downloads": 0
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SearchResult>(stringToDeserialize, _serializerOptions);

        // Assert
        Assert.NotNull(result.Tags);
        Assert.Equal(2, result.Tags.Count);
    }

    #endregion

    #region Authors

    [Fact]
    public void Authors_EmptyArray_ShouldDeserialize()
    {
        // Arrange
        var stringToDeserialize = """
        {
            "id": "https://nuget.pkg.github.com/FakeOrga/fakepkg/1.5.1.json",
            "version": "1.5.1",
            "description": "Package Description",
            "authors": [ ],
            "iconUrl": "",
            "licenseUrl": "",
            "packageTypes": [
                {
                    "name": "myPackageType"
                }
            ],
            "projectUrl": "",
            "registration": "",
            "summary": "",
            "tags": [ "tag1" ],
            "title": "",
            "totalDownloads": 0,
            "versions": [
                {
                    "@id": "",
                    "version": "",
                    "downloads": 0
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SearchResult>(stringToDeserialize, _serializerOptions);

        // Assert
        Assert.NotNull(result.Authors);
        Assert.Empty(result.Authors);
    }

    [Fact]
    public void Authors_EmptyString_ShouldDeserialize()
    {
        // Arrange
        var stringToDeserialize = """
         {
             "id": "https://nuget.pkg.github.com/FakeOrga/fakepkg/1.5.1.json",
             "version": "1.5.1",
             "description": "Package Description",
             "authors": "",
             "iconUrl": "",
             "licenseUrl": "",
             "packageTypes": [
                 {
                     "name": "myPackageType"
                 }
             ],
             "projectUrl": "",
             "registration": "",
             "summary": "",
             "tags": [ "tag1" ],
             "title": "",
             "totalDownloads": 0,
             "versions": [
                 {
                     "@id": "",
                     "version": "",
                     "downloads": 0
                 }
             ]
         }
         """;

        // Act
        var result = JsonSerializer.Deserialize<SearchResult>(stringToDeserialize, _serializerOptions);

        // Assert
        Assert.NotNull(result.Authors);
        Assert.Single(result.Authors);
    }

    [Fact]
    public void Authors_SingleEntry_ShouldDeserializeSingleAuthor()
    {
        // Arrange
        var stringToDeserialize = """
        {
            "id": "https://nuget.pkg.github.com/FakeOrga/fakepkg/1.5.1.json",
            "version": "1.5.1",
            "description": "Package Description",
            "authors": [ "Someone" ],
            "iconUrl": "",
            "licenseUrl": "",
            "packageTypes": [
                {
                    "name": "myPackageType"
                }
            ],
            "projectUrl": "",
            "registration": "",
            "summary": "",
            "tags": [ "tag1" ],
            "title": "",
            "totalDownloads": 0,
            "versions": [
                {
                    "@id": "",
                    "version": "",
                    "downloads": 0
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SearchResult>(stringToDeserialize, _serializerOptions);

        // Assert
        Assert.NotNull(result.Authors);
        Assert.Single(result.Authors);
    }

    [Fact]
    public void Authors_HasMultipleEntries_ShouldDeserializeMultipleAuthors()
    {
        // Arrange
        var stringToDeserialize = """
        {
            "id": "https://nuget.pkg.github.com/FakeOrga/fakepkg/1.5.1.json",
            "version": "1.5.1",
            "description": "Package Description",
            "authors": [ "Someone", "Another Author" ],
            "iconUrl": "",
            "licenseUrl": "",
            "packageTypes": [
                {
                    "name": "myPackageType"
                }
            ],
            "projectUrl": "",
            "registration": "",
            "summary": "",
            "tags": [ "tag1"],
            "title": "",
            "totalDownloads": 0,
            "versions": [
                {
                    "@id": "",
                    "version": "",
                    "downloads": 0
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SearchResult>(stringToDeserialize, _serializerOptions);

        // Assert
        Assert.NotNull(result.Authors);
        Assert.Equal(2, result.Authors.Count);
    }

    #endregion
}
